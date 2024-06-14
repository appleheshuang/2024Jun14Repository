using System;
using System.Net;
using TechTalk.SpecFlow;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using MainFramework.GlobalUtils;
using MainFramework;
using OMREngineTest.Constructors;
using OMREngineTest.ScenarioUtils;
using System.Linq;
using OMREngineTest.ScenarioUtils.Jobs;
using Optimizer.Constructors.ApiResponseObjects;
using Newtonsoft.Json;

namespace OMREngineTest.Utils
{

    public class EngineTest
    {
        public static globalCodes _status_code;

        public readonly ITestOutputHelper _output;

        public QuerySnowflake snowq;
        public OdataApi odataq;
        public SalesForceDataCollector sfc;

        public Dictionary<string,string> switches;

        //defaults
        public const string default_tenantconfigfile = "smoketest-org.json";
        public const int max_commit_time = 1800;                       // for commits wait up to 30 min
        public const int max_start_processing_delay_with_empty_queue = 600; // increased from 1 to 5 to 10 min due to timeout errors
        public const int start_processing_timeout_for_nonempty_queue = 3600; // increased from 10 to 15 to 60 min due to timeout errors
        public const int max_scenario_calculation_time = 900;               // increased from 5 to 10 min due to timeout errors
        public const int max_scenario_sync_time = 1800;                      // increased to 30 min due to timeout errors
        public const int max_queue_load_at_start = 500;
        public const int max_ocesync_time = 15 * 60;
        // overrides
        public string testdatafile; public string initdatafile;
        public string tenantconfigfile;

        public TenantConfig t; public s3TenantFixture tenant; public bool valid_tenant_config = true;

        public eToken engine_token;


        public EngineTest(ITestOutputHelper output)
        {
            _output = output;
            _status_code = new globalCodes("StatusCodes.json");
            //SetTenantConfig(tconfig); //Remove this - this is set in most places
        }

        public bool SetTenantConfig(string tconfig = null)
        {
            tenantconfigfile = tconfig ?? default_tenantconfigfile;

            // Tenant
            try
            {
                tenant = new s3TenantFixture(tenantconfigfile);
                t = tenant.Config;
                bool has_org_credentials = !(t.orgURL == null || t.orgUserName == null || t.orgPassword == null);
                bool has_tenant_credentials = !(t.TenantUser == null || t.TenantPassword == null);
                valid_tenant_config = has_org_credentials || has_tenant_credentials;
            }
            catch (Exception e)
            {
                _output.LogMessage($"The file was not found: '{e}'");
                valid_tenant_config = false;
            }
            if (valid_tenant_config)
            {
                engine_token = GetEngineToken();
                if (t.tenant_exists) odataq = new OdataApi(t, engine_token);
                if (t.snowchecks_enabled) snowq = new QuerySnowflake(t.snowConnection);
            }
            return valid_tenant_config;
        }

        public void Pause(int sec=10)
        {
            System.Threading.Thread.Sleep(sec * 1000);
        }

        public string SubmitStaticSync(bool resync=false, string tables = null)
        {
            string sync_error;
            //sync
            StaticSyncJob _staticSync = new StaticSyncJob(t, engine_token, id: null);
            HttpStatusCode syncjob_reqstatus = _staticSync.StaticSyncTestData(resync,DateTime.Now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss.000Z"), tables);
            bool sync_success = HttpStatusCode.OK == syncjob_reqstatus;
            if (sync_success)
            {
                sync_success = _staticSync.HasStarted(waitime: 600); // allow for up to 10 min queue time
                sync_success = sync_success && _staticSync.IsSuccessful();
                sync_error = "Sync processing error: " + _staticSync.JobStatus() + " -> " + _staticSync.GetJobError();
            }
            else
                sync_error = "Sync submit error: " + syncjob_reqstatus;

            return sync_success ? "OK" : sync_error;
        }

        /// <summary>
        /// Checks the query results for all tests in the given file match expected. 
        /// Used by InitialBulkload + other tests where a scenario is not involved but data tests are wanted
        /// </summary>
        public string RunDataTests(ScenarioContext sc, string file, bool suppress_logging = false, bool sfquery_check = false)
        {
            ScenarioJSONUnmarshaller scenarioUnmarshaller = new ScenarioJSONUnmarshaller(sc, tenantconfigfile);
            TestDefinition datatests = scenarioUnmarshaller.GetTests(file);
            datatests.PrepareTests("OUTPUT");
            ExpectedResultDic expectedResultsDic = datatests.test_dic;
            if (sfquery_check)
                return CheckSfResults(expectedResultsDic, suppress_logging: suppress_logging);
            else
                return CheckSnowResults(expectedResultsDic, suppress_logging: suppress_logging);
        }

        /// <summary>
        /// Returns the prepared ExpectedResultDic
        /// </summary>
        public ExpectedResultDic PrepareTests(ScenarioContext sc, string file, string dataPath = null)
        {
            string testid;
            sc.TryGetValue<string>("TestUniqueIntegrationId", out testid);
            TestDefinition datatests = new JSONUnmarshaller(sc, tenantconfigfile, testid, datapath: dataPath).GetTests(file);
            datatests.PrepareTests("OUTPUT");
            return datatests.test_dic;
        }

        /// <summary>
        /// Returns true if any active reion exists. Option to purge all data - use with caution
        /// </summary>
        /// <param name="reset_data"> Whether to purge all, default to don't purge </param>
        /// <returns> True if there is any pre-existng data </returns>
        public bool PreExistingData(bool reset_data=false)
        {
            if (reset_data)
            {
                if (t.snowchecks_enabled)
                {
                    //purge any existing data from snowflake
                    SnowflakeFunction func = new SnowflakeFunction();
                    func.deleteFromTablesWRecord(t.snowConnection);
                    func.dropExcessSchema(t.snowConnection, "PRECLONE");
                    func.dropExcessSchema(t.snowConnection, "SC_%PUT");
                }
                //purge any existing data from salesforce
                SalesForceHelper sfHelp = new SalesForceHelper(t.clientID, t.clientSecret, t.orgUserName, t.orgPassword, t.orgURL, t.sfnamespace);
                sfHelp.deleteSalesForceData(new SalesForceDataCollector(t).COLLECT_SalesforceIDs());
                return false;
            }
            else if (t.snowchecks_enabled)
            {
                //check for existing regions
                string check_for_any_regions = "select count(*) \"RegCount\" from static.\"OMRegion\" where \"Status\" = 'ACTV'";
                if (int.Parse(snowq.ReturnSingleColumnValueFromSnowFlake(check_for_any_regions, "RegCount")) == 0)
                    return false;
            }
            else  // tenant create org
                return false;
            return true;
        }

        public string CheckSnowResults(ExpectedResultDic _tests, string[] testlist = null, bool suppress_logging=false)
        {
            bool pass = true;
            string failures = "Snowcheck failures for " + t.orgURL + " (" + t.snowConnection + ")...\n";
            if (testlist == null) testlist = new string[1] { "all" };
            if (t.snowchecks_enabled)
            // Check the result of the scenario in snowflake
            {
                if (snowq == null) snowq = new QuerySnowflake(t.snowConnection);
                foreach (KeyValuePair<string, Tuple<string, string>> this_test in _tests.GetTestType("snow"))
                {
                    if (testlist[0].ToLower() == "all" || testlist.Contains(this_test.Key)) 
                    {
                        string query_resp = snowq.GetJsonFormatWithoutUniqkey(this_test.Value.Item1);
                        string ActualResult = _tests.ParseSnowRes(this_test.Key, query_resp);
                        pass = pass && this_test.Value.Item2 == ActualResult;
                        failures += this_test.Value.Item2 == ActualResult ? "" : failure_info("Snow", this_test.Value.Item2, ActualResult, this_test.Key);
                    } 
                }
            }
            else
                failures = "Warning: Snowchecks skipped";
            if (!pass && !suppress_logging) LogDataResult(failures);
            return pass ? "OK" : failures;
        }
        public string CheckOdataResults(ExpectedResultDic _tests, bool committing, string ScenarioId, string[] testlist = null)
        {
            bool pass = true;
            string failures = "Odatacheck failures for "+t.orgURL+" ("+t.snowConnection+")...\n";
            if (testlist == null) testlist = new string[1] { "all" };
            if (odataq == null) odataq = new OdataApi(t, engine_token);

            foreach (KeyValuePair<string, Tuple<string, string>> this_test in _tests.GetTestType("odata"))
            {
                if (testlist[0].ToLower() == "all" || testlist.Contains(this_test.Key))
                {
                    string odata_resp = odataq.GetOdata(this_test.Value.Item1, committing ? "OUTPUT" : _tests.getSchema(), ScenarioId);
                    string odataActual = _tests.ParseOdataRes(this_test.Key, odata_resp);
                    pass = pass && this_test.Value.Item2 == odataActual;
                    failures += this_test.Value.Item2 == odataActual ? "" : failure_info("Odata", this_test.Value.Item2, odataActual, this_test.Key);
                } 
            }
            if (!pass) LogDataResult(failures);
            return pass ? "OK" : failures;
        }
        public string CheckSfResults(ExpectedResultDic _tests, string[] testlist = null, bool oce_test=false, bool suppress_logging=false)
        {
            bool pass = true;
            string failures = "Sfcheck failures  for " + t.orgURL + " (" + t.snowConnection + ")...\n";
            if (testlist == null) testlist = new string[1] { "all" };
            if (sfc == null) sfc = new SalesForceDataCollector(t);
            foreach (KeyValuePair<string, Tuple<string, string>> this_test in _tests.GetTestType("sf")) 
            {
                if (testlist[0].ToLower() == "all" || testlist.Contains(this_test.Key))
                {
                    JArray sfquery_resp = sfc.RUN_SalesforceQuery(this_test.Value.Item1);
                    string actualSF = _tests.ParseSFdata(this_test.Key, sfquery_resp, oce_test ? "OCE" : t.sfnamespace);
                    pass = pass && this_test.Value.Item2 == actualSF;
                    failures += this_test.Value.Item2 == actualSF ? "" : failure_info("SF", this_test.Value.Item2, actualSF, this_test.Key);
                } 
            }
            if (!pass && !suppress_logging) LogDataResult(failures);
            return pass ? "OK" : failures;
        }
        /// <summary>
        /// Execute tests via snowflake browser user
        /// </summary>
        /// <param name="_tests">Set of tests</param>
        /// <param name="testlist">Named subset of tests (or all)</param>
        /// <returns>"OK" is all tests passed, string showing expected v actual otherwise</returns>
        public string CheckSnowRequests(ExpectedResultDic _tests, SnowflakeApi snowReq, string[] testlist = null)
        {
            bool pass = true;
            string failures = "Snow request failures for " + t.orgURL + " (" + t.snowConnection + ")...\n";
            if (testlist == null) testlist = new string[1] { "all" };

            foreach (KeyValuePair<string, Tuple<string, string>> this_test in _tests.GetTestType("snow"))
                if (testlist[0].ToLower() == "all" || testlist.Contains(this_test.Key))
                {
                    string ActualResult = snowReq.QuerySnowflake(this_test.Value.Item1);
                    pass = pass && this_test.Value.Item2 == ActualResult;
                    failures += this_test.Value.Item2 == ActualResult ? "" : failure_info("Browser Request", this_test.Value.Item2, ActualResult, this_test.Key);
                }

            if (!pass) LogDataResult(failures);
            return pass ? "OK" : failures;
        }
        public string CheckDataResults(string jobName, string ScenarioId, ExpectedResultDic _tests, bool committing, bool syncing=false, string named_tests = "all", bool skipSf = false)
        {
            char[] sep = { ',', ';' };
            String[] testnames = named_tests.Split(sep);
            string snow_result = t.snowchecks_enabled ? CheckSnowResults(_tests, testnames) : "OK";
            string odata_result = CheckOdataResults(_tests, committing, ScenarioId, testnames);
            string sf_result = (committing || syncing) && !skipSf ? CheckSfResults(_tests, testnames) : "OK";
            //string sf_result = CheckSfResults(_tests, testnames, syncing);

            bool pass = snow_result == "OK" && odata_result == "OK" && sf_result == "OK";
            string what_failed = (snow_result == "OK" ? "" : " Snow checks failed;") + (odata_result == "OK" ? "" : " Odata checks failed;") + (sf_result == "OK" ? "" : " Sf checks failed;");
            return pass ? "OK" : "Data checks failed for " + jobName + " (" + ScenarioId + ") ..." + what_failed;
        }

        public RootCauseApiResponseBody RCA(RootCauseApiRequestBody rca)
        {
            RootCauseAnalysisJob rcaJob = new RootCauseAnalysisJob(t, engine_token);
            RootCauseApiResponseBody rcaJobResponse = null;
            try
            {
                rcaJobResponse = rcaJob.SubmitRCA(rca);
            } catch (Exception e)
            {
                _output.LogMessage("Failed to deserialize RCA object from the returned JSON. Payload: " + e.Message);
            }
            return rcaJobResponse;
        }

        public void OCESync(OCESyncJobRequestBody parameters, string exp_status = "job_success", int wait_min=0)
        {
            OCESyncJob ocesyncJob = new OCESyncJob(t, engine_token);
            Assert.Equal(HttpStatusCode.OK, ocesyncJob.SubmitOCESyncJob(parameters));
            if (wait_min == 0)
                Assert.True(ocesyncJob.HasCompleted(), "OCESync incomplete after expected wait time");
            else
                Assert.True(ocesyncJob.HasCompleted(wait_min*60), "OCESync incomplete after "+wait_min+" minutes");
            if (exp_status == "job_success")
                Assert.True(ocesyncJob.IsSuccessful(), "Sync failed - latest logs...\n"+ string.Join("\n", ocesyncJob.GetSyncErrors()));
            else
                Assert.Equal(_status_code[exp_status], ocesyncJob.latest_status);
        }

        private string failure_info(string test_type, string exp, string act, string testname="")
        {
            if (testname.Length > 0)
                testname = "[" + testname + "] ";
            return "\t" + testname + test_type +  " exp=" + exp + "\n\t" + testname + test_type + " act=" + act + "\n";
        }

        public eToken GetEngineToken()
        {
            try
            {
                SalesforceApi sftester = new SalesforceApi(t);
                return sftester.GetEngineToken();
            }
            catch
            {
                FatalErrorHandler("Failed to obtain Salesforce token for " + t.orgURL + " | " + t.orgUserName + " | " + t.orgPassword);
                return GetTenantToken();
            }
        }

        public eToken GetTenantToken(bool get_fresh_token=false)
        {
            try
            {
                OptimizerApi tenantester = get_fresh_token ? new OptimizerApi(t) : new OptimizerApi(t, engine_token);
                return tenantester.GetToken();
            }
            catch
            {
                FatalErrorHandler("Failed to obtain engine tenant token for " + t.targetEnv + " | " + t.TenantUser + " | " + t.TenantPassword + " | " + t.sfApiKey);
                return new eToken("", "");
            }
        }

        public string ValidateOrg(string valid_message)
        {
            //Sf org is valid
            string validation_msg = valid_message;
            try
            {
                SalesforceApi sftester = new SalesforceApi(t);
            }
            catch
            {
                validation_msg = "Failed to obtain Salesforce token for " + t.orgURL + " | " + t.orgUserName + " | " + t.orgPassword;
                FatalErrorHandler(validation_msg);
            }
            return validation_msg;
        }

        public string ValidateTenant(string valid_message)
        {
            //Tenant exists
            if (t.tenant_exists)
            {
                //Tenant credentials
                string validation_msg = valid_message;
                string target_credentials = t.orgURL + " | " + t.sfnamespace + " | " + t.orgUserName + " | " + t.orgPassword;
                try
                {
                    // Get engine token from sfdc usng sf access token
                    SalesforceApi sftester = new SalesforceApi(t);
                    engine_token = sftester.GetEngineToken();
                    if (engine_token.token == null) { // with tenant create (sfc doesn't know about the tenant)
                        target_credentials = t.targetEnv + " | " + t.TenantUser + " | " + t.TenantPassword + " | " + t.sfApiKey;
                        new OptimizerApi(t, engine_token);
                    }
                }
                catch
                {
                    validation_msg = "Failed to obtain engine tenant token for " + target_credentials;
                    FatalErrorHandler(validation_msg);
                }
                return validation_msg;
            }
            else
                return "Tenant not created";
        }

        public string ValidateSnowflake(string valid_message)
        {
            //Snowflake connection
            if (t.snowchecks_enabled)
            {
                string validation_msg = valid_message;
                try
                {
                    QuerySnowflake snowtester = new QuerySnowflake(t.snowConnection);
                }
                catch
                {
                    validation_msg = "Failed to connect to snowflake at " + t.snowConnection;
                    FatalErrorHandler(validation_msg);
                }
                return validation_msg;
            }
            else
                return "Snowflake checks disabled";
        }

        public void AccountPrecheck(string sc_aligned_acct)
            // After OMR-5757 is fully implemented - check the old account is inactive
        {
            odataq = new OdataApi(t, engine_token);
            snowq = new QuerySnowflake(t.snowConnection);
            //Ensure account to align is in INAC state
            string account_status;
            try
            {
                string acct_status_req = "/OMAccount?$select=Status&$filter=Id eq '" + sc_aligned_acct + "'";
                account_status = odataq.GetSingleValue("Status", acct_status_req, "STATIC");
                if (account_status == "ACTV")
                {
                    HttpStatusCode odata_patch_resp = odataq.PatchSingleValue("OMAccount", sc_aligned_acct, "Status", "INAC");
                    Assert.True(odata_patch_resp == HttpStatusCode.OK, "Odata patch response = " + odata_patch_resp);
                }
            }
            catch // try again with snowflake
            {
                account_status = snowq.LookupValue("OMAccount", "Status", "Id", sc_aligned_acct, "STATIC");
                if (account_status == "ACTV")
                {
                    Assert.True(snowq.UpdateValue("OMAccount", "Status", "INAC", "Id", sc_aligned_acct, "STATIC"));
                    if (snowq.LookupValue("OMAccount", "Status", "Id", sc_aligned_acct, "STATIC") == "ACTV")
                        FatalErrorHandler("Old Recalc account " + sc_aligned_acct + " is active - Scenario tests will be invalid.");
                }
                throw; // throw the odata error
            }
        }

        public void FatalErrorHandler(string message)
        {
            LogMessage("\n**************************************************  FATAL ERROR  *******************************************************");
            LogMessage("\n\t"+message);
            LogMessage("\n************************************************************************************************************************\n");
        }
        public void LogMessage(string message)
        {
            //Console.WriteLine(message);
            _output.WriteLine(message);
        }
        public void LogDataResult(string message)
        {
            _output.LogMessage(message.Replace("[{","[\n").Replace("},{","\n").Replace("}]","\n]"));
        }
        public void WarningMessage(string message)
        {
            LogMessage("*Warning* "+message);
        }
        public void LogFailure(string test_type, string exp, string act, string testname)
        {
            _output.WriteLine(failure_info(test_type, exp, act, testname));
        }


    }
}