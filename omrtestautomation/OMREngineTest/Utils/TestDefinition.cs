using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework;
using MainFramework.GlobalUtils;
using System.IO;

namespace OMREngineTest.Utils
{
    public class TestDefinition
    {
        //Here is where we set our Data
        private const string rootpath = "TestData";
        private string data_tests;
        private string sf_testdata;

        public Dictionary<string, string> vars;
        public Dictionary<string, string> configs;

        public ExpectedResultDic test_dic;
        public string TestUniqueIntegrationID;
        public string ScenarioId;

        public SalesforceApi sf;
        private FileHelper jfh;
        //public SalesForceDataCollector sfdc;
        private s3TenantFixture tenant;

        public TestDefinition()
        {
            tenant = new s3TenantFixture("smoketest-org.json");
            sf = new SalesforceApi(tenant.Config);
        }

        public TestDefinition(string testname, string testdataFile = "Smoketest/ScenarioData.json", string tenantconfigfile = "smoketest-org.json")
        {
            s3TenantFixture t = new s3TenantFixture(tenantconfigfile);
            sf = new SalesforceApi(t.Config);

            //Ingest the test data ...
            string _testdir = rootpath;
            jfh = new FileHelper();
            var configPath = jfh.GetFilePath(testdataFile, _testdir);
            JObject testfile = JObject.Parse(File.ReadAllText(configPath));
            // ... configs
            JObject test_configs = testfile.ContainsKey("Configs") ? JObject.Parse(testfile["Configs"].ToString()) : null;
            configs = test_configs.ToObject<Dictionary<string, string>>();
            // ... vars to use in the sf data and tests
            JObject test_refs = testfile.ContainsKey("BaseData") ? JObject.Parse(testfile["BaseData"].ToString()) : null;
            vars = test_refs.ToObject<Dictionary<string, string>>();
            // ... sf data and tests
            sf_testdata = testfile.ContainsKey("sfData") ? testfile["sfData"].ToString() : "";
            data_tests = testfile.ContainsKey("Tests") ? testfile["Tests"].ToString() : "";

            data_tests = SetDerivedData(data_tests);

            // Replace the vars with values in the sf data and expected results
            foreach (KeyValuePair<string, string> v in vars)
            {
                sf_testdata = sf_testdata.Replace("{{" + v.Key + "}}", v.Value);
                data_tests = data_tests.Replace("{{" + v.Key + "}}", v.Value);
            }

        }
        public string SetDerivedData(string objstring)
        {
            // Generate the Test Id
            CodeHelper chelp = new CodeHelper();
            TestUniqueIntegrationID = chelp.RandomString(5) + Convert.ToString(new Random().Next(10, 1000)) + DateTime.Now.ToString("yyyyMMdd");

            List<string> regexKeys = FileHelper.ExtractRegex(objstring);
            foreach (string regexKey in regexKeys)
            {
                string regexKeyLowercase = regexKey.ToLower();
                if (regexKeyLowercase.StartsWith("today-"))
                {
                    int day_offset = 0;
                    try { day_offset = int.Parse(regexKeyLowercase["today-".Length..]); }
                    catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                    objstring.Replace(regexKey, chelp.DaysAgo(day_offset));
                    continue;
                }
                if (regexKeyLowercase == "testuniqueintegrationid")
                {
                    objstring.Replace(regexKey, TestUniqueIntegrationID);
                }
            }
            return objstring;
        }
        
        public TestDefinition(string name, string data_tests, string tenantconfigfile, string scenarioId = "")
        {
            this.ScenarioId = scenarioId;
            this.data_tests = data_tests;

            //JObject test_configs = JObject.Parse(stringConfigs);
            //this.configs = test_configs.ToObject<Dictionary<string, string>>();

            this.tenant = new s3TenantFixture(tenantconfigfile);
        }


        public void LoadSfData()
        {
            // Load data into salesforce ...
            Dictionary<string, JArray> sf_data = InitializeDic<JArray>(sf_testdata);
            // ... Scenario first (to get scenario id)
            if ("commit,calculate,scenario".Contains(configs["jobtype"]))
            {
                Scenario _scenario = JsonConvert.DeserializeObject<Scenario>(sf_data["Scenario"][0].ToString());
                ScenarioId = sf.Post_Object("OMScenario__c", _scenario);
                sf_data.Remove("Scenario");
            }
            // ... then all other sf data
            foreach (KeyValuePair<string, JArray> table_data in sf_data)
            {
                string table_name = table_data.Key;
                //JArray table_recs = JArray.Parse(table_data.Value.ToString().Replace("{{ScenarioId}}", ScenarioId));
                LoadSFTableRecords(table_name, table_data.Value, ScenarioId);
            }
        }

        public void PrepareTests(string db_schema)
        {
            test_dic = new ExpectedResultDic(tenant.Config.snowConnection, db_schema, tenant.Config.sfnamespace);
            Dictionary<string, JArray> tests = InitializeDic<JArray>(data_tests);
            QuerySnowflake snowq = null;
            if (tenant.Config.snowchecks_enabled) snowq = new QuerySnowflake(tenant.Config.snowConnection);

            //Load merge tests
            if (tests.ContainsKey("MergeResults"))
            {
                JArray merge_tests = JArray.Parse(tests["MergeResults"].ToString());
                foreach (JObject t in merge_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    test_dic.AddQueryResult(
                        this_test["testname"],
                        this_test.ContainsKey("query") ? this_test["query"] : "skip",
                        this_test["exp_result"],
                        this_test.ContainsKey("odataQuery") ? this_test["odataQuery"] : "skip",
                        this_test.ContainsKey("sfQuery") ? this_test["sfQuery"] : "skip"
                    );
                }
            }

            //Load alignment tests
            if (tests.ContainsKey("AlignmentResults"))
            {
                JArray alignment_tests = JArray.Parse(tests["AlignmentResults"].ToString());
                foreach (JObject t in alignment_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    string account_ids = tenant.Config.snowchecks_enabled ? snowq.GetValuesAsCsv("OMAccount","Id","UniqueIntegrationId", this_test["account_uids"], "STATIC") : null;
                    AlignmentQueryResult align_test = new AlignmentQueryResult(
                        this_test["territory_uids"],
                        this_test["account_uids"],
                        this_test["exp_result"],
                        this_test["territory_somids"],
                        account_ids
                    );
                    test_dic.AddQueryResult(this_test["testname"], align_test);
                }
            }
            //Load geography alignment tests
            if (tests.ContainsKey("GeographyAlignments"))
            {
                JArray alignment_tests = JArray.Parse(tests["GeographyAlignments"].ToString());
                foreach (JObject t in alignment_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    GeoAlignmentQueryResult align_test = new GeoAlignmentQueryResult(
                        this_test["territory_uids"],
                        this_test["geography_uids"],
                        this_test["exp_result"],
                        extra_columns: this_test.ContainsKey("extra_columns") ? this_test["extra_columns"] : null
                    );
                    test_dic.AddQueryResult(this_test["testname"], align_test);
                }
            }

            //Load other account-terr tests
            if (tests.ContainsKey("Explicits"))
            {
                JArray act_tests = JArray.Parse(tests["Explicits"].ToString());
                foreach (JObject t in act_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>(); 
                    test_dic.AddQueryResult(this_test["testname"], PrepareAccountMappingTest(this_test, snowq, "OMAccountExplicit", "Territory"));
                }
            }
            //Load other account-entity tests
            if (tests.ContainsKey("Exclusions"))
            {
                JArray act_tests = JArray.Parse(tests["Exclusions"].ToString());
                foreach (JObject t in act_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    test_dic.AddQueryResult(this_test["testname"], PrepareAccountMappingTest(this_test, snowq, "OMAccountExclusion", "Territory"));
                }
            }
            //Load other account-entity tests
            if (tests.ContainsKey("SfExclusions"))
            {
                JArray act_tests = JArray.Parse(tests["SfExclusions"].ToString());
                foreach (JObject t in act_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    test_dic.AddQueryResult(this_test["testname"], PrepareAccountMappingTest(this_test, snowq, "OMAccountExclusion", "SalesForce"));
                }
            }

            //Load scenario summary tests
            if (tests.ContainsKey("ScenarioSummary"))
            {
                JArray summary_tests = JArray.Parse(tests["ScenarioSummary"].ToString());
                SummaryQueryResult summary = new SummaryQueryResult(ScenarioId);
                foreach (JObject t in summary_tests)
                {
                    Dictionary<string, string> acct_type = t.ToObject<Dictionary<string, string>>();
                    summary.AddSummaryResult(
                                  acct_type["territory"],
                                  acct_type["accounttype"],
                        int.Parse(acct_type["acc_gain"]),
                        int.Parse(acct_type["acc_lost"]),
                        int.Parse(acct_type["geo_gain"]),
                        int.Parse(acct_type["geo_lost"]),
                        int.Parse(acct_type["acc_tot"]),
                        int.Parse(acct_type["geo_tot"])
                    );
                    test_dic.AddQueryResult("Scenario summary", summary);
                }

            }

            //Load OCE tests
            if (tests.ContainsKey("OCEResults"))
            {
                JArray oce_tests = JArray.Parse(tests["OCEResults"].ToString());
                foreach (JObject t in oce_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    OCEQueryResult oce_test =
                        this_test.ContainsKey("query")
                            ? new OCEQueryResult(this_test["sfQuery"], this_test["exp_result"], "OCE", this_test["query"])
                            : new OCEQueryResult(this_test["sfQuery"], this_test["exp_result"], "OCE");
                    test_dic.AddQueryResult(this_test["testname"], oce_test);
                }
            }

            //Timestamp tests
            if (tests.ContainsKey("TimestampResults"))
            {
                JArray timestamp_tests = JArray.Parse(tests["TimestampResults"].ToString());
                foreach (JObject t in timestamp_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    string comparison_query = this_test.ContainsKey("compare_to") ? trim_terminator(this_test["compare_to"]) : null;
                    string test_query = trim_terminator(this_test["query"]);
                    bool syncing = false;
                    if (this_test.ContainsKey("sync")) if (this_test["sync"].ToLower() == "true") syncing = true;

                    TimestampQueryResult timestamp_test;
                    if ((comparison_query ?? "").ToLower().StartsWith(TimestampQueryResult.use_scenario_timestamp))
                    {
                        int scenario_id_pos = comparison_query.IndexOf(':') + 1;
                        string scenario_id = comparison_query.Substring(scenario_id_pos, comparison_query.Length - scenario_id_pos);
                        timestamp_test = this_test.ContainsKey("allowed_timediff_sec")
                            ? new TimestampQueryResult(test_query, ScenarioId: scenario_id, syncing, max_elapsed: this_test["allowed_timediff_sec"])
                            : new TimestampQueryResult(test_query, ScenarioId: scenario_id, syncing);
                    }
                    else
                        timestamp_test = this_test.ContainsKey("allowed_timediff_sec")
                            ? new TimestampQueryResult(test_query, compare_to: comparison_query, max_elapsed: this_test["allowed_timediff_sec"])
                            : new TimestampQueryResult(test_query, compare_to: comparison_query);
                    
                    test_dic.AddQueryResult(this_test["testname"], timestamp_test);
                }
            }
            //QueryTag tests
            if (tests.ContainsKey("QueryTag"))
            {
                JArray querytag_tests = JArray.Parse(tests["QueryTag"].ToString());
                foreach (JObject t in querytag_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    string elapsed_time_min = CheckIntOrDefault(this_test,"elapsed_min", "5");
                    string query_tag;
                    try
                    {
                        query_tag = this_test["query_tag"];
                    }
                    catch
                    {
                        throw new Exception("QueryTag test must specify the query_tag to match on.");
                    }
                    string query_text = this_test.ContainsKey("sql") ? trim_terminator(this_test["sql"]) : null;
                    string row_count = CheckIntOrDefault(this_test, "rows", "1");

                    QueryHistory queryhist_test = new QueryHistory(query_tag, elapsed_time_min, row_count, query_text);

                    test_dic.AddQueryResult(this_test["testname"], queryhist_test);
                }
            }
            //Volume load tests
            if (tests.ContainsKey("VolumeCheck"))
            {
                JArray volume_tests = JArray.Parse(tests["VolumeCheck"].ToString());
                foreach (JObject t in volume_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    int volume_threshold; string table_name;
                    try
                    {
                        volume_threshold = int.Parse(this_test["min_record_count"]);
                        table_name = this_test["table"];
                    }
                    catch
                    {
                        throw new Exception("VolumeCheck test '" + this_test["testname"] + "' must have table and min_record_count (int).");
                    }
                    VolumeResult volume_test = new VolumeResult(table_name, volume_threshold.ToString());
                    test_dic.AddQueryResult(this_test["testname"], volume_test);
                }
            }
            //Scenaro validation tests
            if (tests.ContainsKey("ValidationErrors"))
            {
                JArray these_tests = JArray.Parse(tests["ValidationErrors"].ToString());
                foreach (JObject t in these_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    test_dic.AddQueryResult(
                        this_test["testname"],
                        this_test.ContainsKey("Item1") 
                            ? new ValidationError(
                                this_test["table"],
                                this_test["ErrorCode"],
                                this_test["ErrorMessage"],
                                this_test["Item1"],
                                this_test["Item2"],
                                this_test.ContainsKey("PKey") ? this_test["PKey"] : null
                                )
                            : new ValidationError(
                                this_test["table"],
                                this_test["ErrorCode"],
                                this_test["ErrorMessage"],
                                this_test.ContainsKey("PKey") ? this_test["PKey"] : null
                                )
                        );
                }
            }
            //Enagagement plan result tests
            if (tests.ContainsKey("EngagementResults"))
            {
                JArray engplan_tests = JArray.Parse(tests["EngagementResults"].ToString());
                List<string> valid_target_tables = new List<string> { "Result", "Explicit" };
                foreach (JObject t in engplan_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    string account_ids = tenant.Config.snowchecks_enabled ? snowq.GetValuesAsCsv("OMAccount", "Id", "UniqueIntegrationId", this_test["account_uids"], "STATIC") : null;
                    List<string> tables = new List<string> { "Result" };
                    if (this_test.ContainsKey("type"))
                        tables = this_test["type"].Split(',').ToList();
                    foreach (string resulttype in tables)
                    { 
                        if (valid_target_tables.Contains(resulttype)) 
                            test_dic.AddQueryResult(
                                this_test["testname"] + "_" + resulttype,
                                new EngagementResult(
                                    tablename: "OMEngagement" + resulttype,
                                    engplanSOMId: this_test["engplan_somid"],
                                    territorySOMId: this_test["territory_somid"],
                                    accountUID: this_test["account_uids"],
                                    exp_accounts: this_test.ContainsKey("exp_account_uids") ? this_test["exp_account_uids"] : this_test["account_uids"],
                                    exp_territory: this_test["exp_territory_uid"],
                                    exp_segId: this_test["exp_segment_id"],
                                    exp_target: this_test["exp_target"]
                                )
                             );
                    }
                }
            }

            //Metric Summary tests
            if (tests.ContainsKey("MetricSummary"))
            {
                JArray summary_tests = JArray.Parse(tests["MetricSummary"].ToString());
                foreach (JObject t in summary_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    test_dic.AddQueryResult(
                                this_test["testname"],
                                new MetricSummaryResult(
                                    ScenarioId,
                                    acctTypes: this_test.ContainsKey("accounttype") ? this_test["accounttype"] : "Total",
                                    territoryUId: this_test["territory"],
                                    columns: this_test["columns"],
                                    expected: this_test["exp_result"],
                                    precision: this_test.ContainsKey("precision") ? int.Parse(this_test["precision"]) : 3,
                                    useoldmethod: this_test.ContainsKey("method") && (this_test["method"].ToLower() == "old" || this_test["method"].ToLower() == "scenariosummary")
                                    )
                                );
                }

            }
            //Metric Result tests
            if (tests.ContainsKey("MetricResult"))
            {
                JArray summary_tests = JArray.Parse(tests["MetricResult"].ToString());
                foreach (JObject t in summary_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    XXXEntity metric_entity = new XXXEntity(entity_name: CapsCase(this_test["entity"]), type: CapsCase(this_test.ContainsKey("type") ? this_test["type"] : null));

                    test_dic.AddQueryResult(
                                this_test["testname"],
                                new MetricResult(
                                    entityName: metric_entity.entityName,
                                    metricTable: metric_entity.xxxTable,
                                    metricEntityIdCol: metric_entity.fkColumn,
                                    entityTable: metric_entity.entityTable,
                                    entityIdCol: metric_entity.entityIdColumn,
                                    salesforceSOMId: this_test["salesforceId"],
                                    entityId: this_test["entityId"],
                                    columns: this_test["columns"],
                                    expected: this_test["exp_result"],
                                    replaceNullWith: this_test.ContainsKey("nullAs") ? this_test["nullAs"] : null
                                    )
                                );
                }

            }
            //Empty Metric tests
            if (tests.ContainsKey("EmptyMetrics"))
            {
                JArray summary_tests = JArray.Parse(tests["EmptyMetrics"].ToString());
                foreach (JObject t in summary_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    XXXEntity metric_entity = new XXXEntity(entity_name: CapsCase(this_test["entity"]), type: CapsCase(this_test.ContainsKey("type") ? this_test["type"] : null));

                    test_dic.AddQueryResult(
                                this_test["testname"],
                                new EmptyResult(
                                    entityName: metric_entity.entityName,
                                    metricTable: metric_entity.xxxTable,
                                    metricEntityIdCol: metric_entity.fkColumn,
                                    entityTable: metric_entity.entityTable,
                                    entityIdCol: metric_entity.entityIdColumn,
                                    salesforceSOMId: this_test["salesforceId"],
                                    entityId: this_test["entityId"],
                                    columns: this_test["columns"]
                                    )
                                );
                }

            }
            //Adjudication User Assignment tests
            if (tests.ContainsKey("AdjUserAssignment"))
            {
                JArray assign_tests = JArray.Parse(tests["AdjUserAssignment"].ToString());
                foreach (JObject t in assign_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();

                    test_dic.AddQueryResult(
                                this_test["testname"],
                                new AdjUserAssignment(this_test["adjudicationId"],this_test["exp_result"])
                                );
                }
            }
            //Adjudication Result tests
            if (tests.ContainsKey("AdjudicationResult"))
            {
                JArray summary_tests = JArray.Parse(tests["AdjudicationResult"].ToString());
                foreach (JObject t in summary_tests)
                {
                    Dictionary<string, string> this_test = t.ToObject<Dictionary<string, string>>();
                    XXXEntity adj_entity = new XXXEntity(entity_name: CapsCase(this_test["entity"]), table_suffix: "AdjudicationResult");

                    test_dic.AddQueryResult(
                                this_test["testname"],
                                new AdjudicationResult(
                                    entityName: adj_entity.entityName,
                                    adjTable: adj_entity.xxxTable.Replace("{{schema}}","STATIC"),
                                    adjEntityIdCol: adj_entity.fkColumn,
                                    entityTable: adj_entity.entityTable,
                                    entityIdCol: adj_entity.entityIdColumn,
                                    adjudicationid: this_test["adjudicationId"],
                                    entityUId: this_test.ContainsKey("entityUId") ? this_test["entityUId"] : null,
                                    columns: this_test["columns"],
                                    expected: this_test["exp_result"],
                                    territoryUId: this_test.ContainsKey("territoryUId") ? this_test["territoryUId"] : null,
                                    role: this_test.ContainsKey("role") ? this_test["role"] : "REQ",
                                    replaceNullWith: this_test.ContainsKey("nullAs") ? this_test["nullAs"] : null
                                    )
                                );
                }
            }

        }

        public struct XXXEntity
        {
            public string entityName;
            public string xxxTable;
            public string fkColumn;
            public string entityTable;
            public string entityIdColumn;
            public XXXEntity(string entity_name, string table_suffix = "Metric",string type = "Current")
            {
                entityName = entity_name;
                fkColumn = "SOM" + entity_name + "Id";
                entityIdColumn = fkColumn;
                string entity_db_schema = "STATIC";
                if (entity_name == "Account")
                {
                    fkColumn = "OM" + entity_name + "Id";
                    entityIdColumn = "Id";
                }
                else if (entity_name == "Territory")
                    entity_db_schema = "{{schema}}";

                xxxTable = "{{schema}}." + "\"OM" + entity_name + table_suffix + "\"";
                if (type != "" && type != "Current")
                    xxxTable = "{{inputschema}}.\"" + entity_name + table_suffix + type + "\"";
                 entityTable = entity_db_schema + "." + "\"OM" + entity_name + "\"";
            }
        }

        private AccountMappingQueryResult PrepareAccountMappingTest(Dictionary<string, string> this_test, QuerySnowflake snowq, string table_name=null, string entity_name=null) 
        {
            string account_ids = tenant.Config.snowchecks_enabled ? snowq.GetValuesAsCsv("OMAccount", "Id", "UniqueIntegrationId", this_test["account_uids"], "STATIC") : null;
            bool include_inactive_records = this_test.ContainsKey("show_inactive") ? bool.Parse(this_test["show_inactive"]) : false;
            return new AccountMappingQueryResult(
                table_name ?? this_test["table_name"],
                entity_name ?? this_test["entity_name"],
                this_test[entity_name.ToLower() + "_uids"],
                this_test["account_uids"],
                this_test["exp_result"],
                this_test[entity_name.ToLower() + "_somids"],
                account_ids,
                extra_columns: this_test.ContainsKey("extra_columns") ? this_test["extra_columns"] : null,
                show_inac: include_inactive_records
            );
        }

        private string trim_terminator(string sql, char terminator = ';')
        {
            return sql.EndsWith(terminator) ? sql.Substring(0, sql.Length - 1) : sql;
        }
        private string CapsCase(string input="")
        {
            if (input == null || input.Length == 0)
                return "";
            else if (input.Length == 1)
                return input.ToUpper();
            else
                return input[0].ToString().ToUpper() + input.Substring(1).ToLower();
        }
        private string CheckIntOrDefault(Dictionary<string, string> dic, string key, string default_value) {
            try
            {
                int i = dic.ContainsKey(key) ? int.Parse(dic[key]) : int.Parse(default_value);
                return i.ToString();
            }
            catch
            {
                throw new Exception("Expecting integer value for " + key + ". Got "+ dic[key]);
            }
        }

        private Dictionary<string,T> InitializeDic<T>(string keyvalues)
        {
            return keyvalues == "" ? new Dictionary<string, T>() : JObject.Parse(keyvalues).ToObject<Dictionary<string, T>>();
        }

        // Post new scenario data
        private string LoadSFTableRecords(string table, JArray content, string sc_id=null)
        {
            string newobj_id = "";
            string sf_table = "OM" + table + "__c";
            Object post_obj = null;
            foreach (JObject rec in content) {
                if (table.StartsWith("Scenario")) rec["OMScenarioId__c"] = sc_id;
                switch (table) {
                case "Scenario":
                    post_obj = JsonConvert.DeserializeObject<Scenario>(rec.ToString());
                    break;
                case "ScenarioSalesForce":
                    post_obj = JsonConvert.DeserializeObject<ScenarioSalesForce>(rec.ToString());
                    break;
                case "ScenarioTerritory":
                    post_obj = JsonConvert.DeserializeObject<ScenarioTerritory>(rec.ToString());
                    break;
                case "ScenarioGeographyTerritory":
                    post_obj = JsonConvert.DeserializeObject<ScenarioGeographyTerritory>(rec.ToString());
                    break;
                case "ScenarioRule":
                    post_obj = JsonConvert.DeserializeObject<ScenarioRule>(rec.ToString());
                    break;
                case "ScenarioSalesForceHierarchy":
                    post_obj = JsonConvert.DeserializeObject<ScenarioSalesForceHierarchy>(rec.ToString());
                    break;
                case "ScenarioTerritoryHierarchy":
                    post_obj = JsonConvert.DeserializeObject<ScenarioTerritoryHierarchy>(rec.ToString());
                    break;
                case "ScenarioAccountExclusion":
                    post_obj = JsonConvert.DeserializeObject<ScenarioAccountExclusion>(rec.ToString());
                    break;
                case "ScenarioAccountExplicit":
                    post_obj = JsonConvert.DeserializeObject<ScenarioAccountExplicit>(rec.ToString());
                    break;
                case "ScenarioAccountOwner":
                    post_obj = JsonConvert.DeserializeObject<ScenarioAccountOwner>(rec.ToString());
                    break;
                case "ScenarioAccountProductExplicit":
                    post_obj = JsonConvert.DeserializeObject<ScenarioAccountProductExplicit>(rec.ToString());
                    break;
                case "ScenarioAccountProductRule":
                    post_obj = JsonConvert.DeserializeObject<ScenarioAccountProductRule>(rec.ToString());
                    break;
                case "ScenarioAccountProductTerrExplicit":
                    post_obj = JsonConvert.DeserializeObject<ScenarioAccountProductTerrExplicit>(rec.ToString());
                    break;
                case "ScenarioAccountSalesForceExclusion":
                    post_obj = JsonConvert.DeserializeObject<ScenarioAccountSalesForceExclusion>(rec.ToString());
                    break;
                case "ScenarioProductExclusion":
                    post_obj = JsonConvert.DeserializeObject<ScenarioProductExclusion>(rec.ToString());
                    break;
                case "ScenarioProductExplicit":
                    post_obj = JsonConvert.DeserializeObject<ScenarioProductExplicit>(rec.ToString());
                    break;
                case "ScenarioProductSalesForce":
                    post_obj = JsonConvert.DeserializeObject<ScenarioProductSalesForce>(rec.ToString());
                    break;
                case "ScenarioEngagementPlan":
                    post_obj = JsonConvert.DeserializeObject<ScenarioEngagementPlan>(rec.ToString());
                    break;
                case "ScenarioUserAssignment":
                    post_obj = JsonConvert.DeserializeObject<ScenarioUserAssignment>(rec.ToString());
                    break;
                }

                newobj_id = sf.Post_Object(sf_table, post_obj);
                // Get the SOM Id for the scenario record. Replace references in the test data
                if (rec.ContainsKey("UniqueIntegrationId__c") && table.StartsWith("Scenario"))
                {
                    string somid = sf.ScenarioSOMId(sf_table, newobj_id);
                    string uid = rec["UniqueIntegrationId__c"].ToString();
                    data_tests.Replace("{{somlookup-" + table + ":" + uid + "}}", somid);
                }

            }
            return newobj_id;
        }

    }
}
