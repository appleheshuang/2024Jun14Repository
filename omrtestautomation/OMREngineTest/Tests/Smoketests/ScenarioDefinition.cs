using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework;
using MainFramework.GlobalUtils;
using System.IO;

namespace OMREngineTest.Utils
{
    public class ScenarioDefinition
    {
        //Here is where we set our Data
        private const string rootpath = "TestData/";

        public ExpectedResultDic test_dic;
        public string ScenarioId { get; }
        public string ScenarioUniqueIntegrationID;
        public s3TenantFixture tenant;
        public Scenario smoke_scenario;
        public SalesforceApi sf;

        private FileHelper jfh;

        public string db_schema;
        private string salesforceUID, territoryUID, ruleUID;

        public ScenarioDefinition()
        {
            tenant = new s3TenantFixture("smoketest-org.json");
            sf = new SalesforceApi(tenant.Config);
        }

        public ScenarioDefinition(string scenarioname, string testdataFile = "Snoketest/ScenarioData.json", string tenantconfigfile = "smoketest-org.json")
        {
            // Tenant info
            tenant = new s3TenantFixture(tenantconfigfile);
            string snowdb = tenant.Config.snowConnection;
            sf = new SalesforceApi(tenant.Config);

            //Test data
            string _testdir = rootpath;
            jfh = new FileHelper();

            var configPath = jfh.GetFilePath(testdataFile, _testdir);
            JObject testfile = JObject.Parse(File.ReadAllText(configPath));

            JObject baseData = JObject.Parse(testfile["BaseData"].ToString());

            CodeHelper chelp = new CodeHelper();
            ScenarioUniqueIntegrationID = chelp.RandomString(5) + Convert.ToString(new Random().Next(10, 1000)) + DateTime.Now.ToString("yyyyMMdd");

            // BASEDATA used in scenario
            string som_region_id = baseData["SOMRegionId"].ToString();
            string aligned_accountUId = baseData["aligned_accountUId"].ToString(); //"50";
            string explicit_accountUId = baseData["explicit_accountUId"].ToString();  //45";
            string explrule_accountUId = baseData["explrule_accountUId"].ToString(); // 51
            string exclude_accountUId = baseData["exclude_accountUId"].ToString(); //"10";
            salesforceUID = baseData["salesforceUID"].ToString() + ScenarioUniqueIntegrationID; //"SF405-" + ScenarioUniqueIntegrationID;
            territoryUID = baseData["territoryUID"].ToString() + ScenarioUniqueIntegrationID; //"T505-" + ScenarioUniqueIntegrationID;
            ruleUID = baseData["ruleUID"].ToString() + ScenarioUniqueIntegrationID; //"R201-" + ScenarioUniqueIntegrationID;

            //Prepare scenario
            Scenario smoke_scenario = JsonConvert.DeserializeObject<Scenario>(testfile["Scenario"].ToString());
            smoke_scenario.SetFieldsToUniqueIntegrationId(scenarioname, ScenarioUniqueIntegrationID);
            ScenarioId = PostScenarioToSF("OMScenario__c", smoke_scenario);

            //SalesForce Data
            ScenarioSalesForce salesforce = JsonConvert.DeserializeObject<ScenarioSalesForce>(testfile["SalesForceData"].ToString());
            salesforce.OMScenarioId__c = ScenarioId;
            salesforce.UniqueIntegrationId__c = salesforceUID;
            salesforce.SOMRegionId__c = som_region_id;
            PostScenarioToSF("OMScenarioSalesForce__c", salesforce);

            //Territory Data
            ScenarioTerritory territory = JsonConvert.DeserializeObject<ScenarioTerritory>(testfile["TerritoryData"].ToString());
            territory.OMScenarioId__c = ScenarioId;
            territory.SalesForceUniqueIntegrationId__c = salesforceUID;
            territory.UniqueIntegrationId__c = territoryUID;
            territory.SOMRegionId__c = som_region_id;
            PostScenarioToSF("OMScenarioTerritory__c", territory);
            string terrSOMId = sf.ScenarioSOMId("OMScenarioTerritory__c", territoryUID, "UniqueIntegrationId__c");

            //GeographyTerritory Data
            ScenarioGeographyTerritory geoterr = JsonConvert.DeserializeObject<ScenarioGeographyTerritory>(testfile["GeographyTerritoryData"].ToString());
            geoterr.OMScenarioId__c = ScenarioId;
            geoterr.SOMTerritoryId__c = terrSOMId;
            geoterr.SOMGeographyTerritoryId__c = geoterr.SOMGeographyId__c + ScenarioUniqueIntegrationID;
            geoterr.UniqueIntegrationId__c = geoterr.SOMGeographyTerritoryId__c;
            PostScenarioToSF("OMScenarioGeographyTerritory__c", geoterr);

            //Rule data
            ScenarioRule rule = JsonConvert.DeserializeObject<ScenarioRule>(testfile["RuleData"].ToString());
            rule.Name = "Smoketest " + ruleUID;
            rule.UniqueIntegrationId__c = ruleUID;
            rule.OMScenarioId__c = ScenarioId;
            rule.SalesForceUniqueIntegrationId__c = salesforceUID;
            string scRuleId = PostScenarioToSF("OMScenarioRule__c", rule);
            string ruleSOMId = sf.ScenarioSOMId("OMScenarioRule__c", scRuleId);

            //AccountExplicitData
            ScenarioAccountExplicit sae = JsonConvert.DeserializeObject<ScenarioAccountExplicit>(testfile["AccountExplicitData"].ToString());
            sae.AccountUniqueIntegrationId__c = explicit_accountUId;
            sae.TerritoryUniqueIntegrationId__c = territoryUID;
            sae.OMScenarioId__c = ScenarioId;
            PostScenarioToSF("OMScenarioAccountExplicit__c", sae);
            sae.AccountUniqueIntegrationId__c = explrule_accountUId; //2nd account explicit for same terr
            PostScenarioToSF("OMScenarioAccountExplicit__c", sae);

            //AccountExclusionData
            ScenarioAccountExclusion sael = JsonConvert.DeserializeObject<ScenarioAccountExclusion>(testfile["AccountExclusionData"].ToString());
            sael.AccountUniqueIntegrationId__c = exclude_accountUId;
            sael.TerritoryUniqueIntegrationId__c = territoryUID;
            sael.OMScenarioId__c = ScenarioId;
            PostScenarioToSF("OMScenarioAccountExclusion__c", sael);

            // initialize tests
            db_schema = "SC_" + ScenarioId.ToUpper() + "_OUTPUT";
            test_dic = new ExpectedResultDic(snowdb, db_schema,tenant.Config.sfnamespace);

            //Prepare the query and expected result to match the scenario
            string query  = "OMTerritory,Name,Status,EffectiveDate,EndDate,UniqueIntegrationId,UniqueIntegrationId:'" + territoryUID + "'";
            string exp_result = "[{Name:" + territory.Name + ",Status:ACTV,EffectiveDate:" + smoke_scenario.EffectiveDate__c + "T00:00:00Z,EndDate:" + smoke_scenario.EndDate__c + "T00:00:00Z,UniqueIntegrationId:"+ territoryUID + "}]";
            string odataQuery = "/OMTerritory?$select=Name,Status,EffectiveDate,EndDate,UniqueIntegrationId&$filter=UniqueIntegrationId eq '" + territoryUID + "'";
            test_dic.AddQueryResult("merge_territory", query, exp_result, odataQuery);

            query = "select gt.\"SOMGeographyId\", t.\"Name\", t.\"UniqueIntegrationId\", "
                + "gt.\"Status\", gt.\"EffectiveDate\", gt.\"EndDate\" from {{schema}}.\"OMGeographyTerritory\" gt "
                + "join {{schema}}.\"OMTerritory\" t on t.\"SOMTerritoryId\" = gt.\"SOMTerritoryId\" "
                // + "join static.\"OMGeograph\y" g on g.\"SOMGeographyId\" = ga.\"SOMGeographyId\" "
                + "where gt.\"UniqueIntegrationId\" = '"+ geoterr.SOMGeographyTerritoryId__c+"';";
            exp_result = "[{SOMGeographyId:" + geoterr.SOMGeographyId__c + ",Name:" + territory.Name + ",UniqueIntegrationId:" + territoryUID+",Status:ACTV,EffectiveDate:" + smoke_scenario.EffectiveDate__c + "T00:00:00Z,EndDate:" + smoke_scenario.EndDate__c + "T00:00:00Z}]";
            string sf_query = "select {{prefix}}OMGeographyId__r.{{prefix}}SOMGeographyId__c, {{prefix}}OMTerritoryId__r.Name, {{prefix}}OMTerritoryId__r.{{prefix}}UniqueIntegrationId__c, "
                + "{{prefix}}Status__c, {{prefix}}EffectiveDate__c, {{prefix}}EndDate__c from {{prefix}}OMGeographyTerritory__c "
                + "where {{prefix}}UniqueIntegrationId__c = '" + geoterr.SOMGeographyTerritoryId__c + "'";
            test_dic.AddQueryResult("merge_geoterr", query, exp_result, sf:sf_query);

            string explicit_alignment = "TerritoryUID:" + territoryUID + ",AccountUID:" + explicit_accountUId + ",EffectiveDate:" + smoke_scenario.EffectiveDate__c + "T00:00:00Z,EndDate:" + smoke_scenario.EndDate__c + "T00:00:00Z,Source:EXPL,Reason:";
            string rule_alignment = "TerritoryUID:" + territoryUID + ",AccountUID:" + aligned_accountUId + ",EffectiveDate:" + smoke_scenario.EffectiveDate__c + "T00:00:00Z,EndDate:" + smoke_scenario.EndDate__c + "T00:00:00Z,Source:DSHR,Reason:" + rule.Name + "|" + ruleSOMId;
            string explrule_alignment = "TerritoryUID:" + territoryUID + ",AccountUID:" + explrule_accountUId + ",EffectiveDate:" + smoke_scenario.EffectiveDate__c + "T00:00:00Z,EndDate:" + smoke_scenario.EndDate__c + "T00:00:00Z,Source:DSHR;EXPL,Reason:" + rule.Name + "|" + ruleSOMId;
            AlignmentQueryResult smoke_align = new AlignmentQueryResult(territoryUID, explicit_accountUId, "[{" + explicit_alignment + "}]", terrSOMId, explicit_accountUId);
            smoke_align.AddAccountFilter(aligned_accountUId);
            smoke_align.AddAccountFilter(explrule_accountUId);
            smoke_align.AddAccountFilter(exclude_accountUId);
            smoke_align.AddResult(explrule_alignment);
            smoke_align.AddResult(rule_alignment);
            test_dic.AddQueryResult("alignments", smoke_align);

            JObject scnSummary = JObject.Parse(testfile["ScenarioSummary"].ToString());
            JObject summDept = JObject.Parse(scnSummary["Department"].ToString());
            JObject summInstitution = JObject.Parse(scnSummary["Institution"].ToString());
            JObject summTotal = JObject.Parse(scnSummary["Total"].ToString());
            SummaryQueryResult smoke_summary = new SummaryQueryResult(ScenarioId);
            smoke_summary.AddSummaryResult(territoryUID,
                "Department",
                int.Parse(summDept["acc_gain"].ToString()),
                int.Parse(summDept["acc_lost"].ToString()),
                int.Parse(summDept["geo_gain"].ToString()),
                int.Parse(summDept["geo_lost"].ToString()),
                int.Parse(summDept["acc_tot"].ToString()),
                int.Parse(summDept["geo_tot"].ToString())
                );
            smoke_summary.AddSummaryResult(territoryUID,
                  "Institution",
                  int.Parse(summInstitution["acc_gain"].ToString()),
                  int.Parse(summInstitution["acc_lost"].ToString()),
                  int.Parse(summInstitution["geo_gain"].ToString()),
                  int.Parse(summInstitution["geo_lost"].ToString()),
                  int.Parse(summInstitution["acc_tot"].ToString()),
                  int.Parse(summInstitution["geo_tot"].ToString())
                  );
            smoke_summary.AddSummaryResult(territoryUID,
                  "Total",
                  int.Parse(summTotal["acc_gain"].ToString()),
                  int.Parse(summTotal["acc_lost"].ToString()),
                  int.Parse(summTotal["geo_gain"].ToString()),
                  int.Parse(summTotal["geo_lost"].ToString()),
                  int.Parse(summTotal["acc_tot"].ToString()),
                  int.Parse(summTotal["geo_tot"].ToString())
                  );
            test_dic.AddQueryResult("summary", smoke_summary);

/*            AlignmentQueryResult rules_disabled = new AlignmentQueryResult(territoryUID, null, expected: null);
            test_dic.AddQueryResult("rules_disabled", rules_disabled);*/
        }

        public string PrepareScenario(Scenario scenario)
        {
            CodeHelper chelp = new CodeHelper();

            ScenarioUniqueIntegrationID = chelp.RandomString(5) + Convert.ToString(new Random().Next(10, 1000)) + DateTime.Now.ToString("yyyyMMdd");
            scenario.SetFieldsToUniqueIntegrationId(scenario.Name, ScenarioUniqueIntegrationID);
            return PostScenarioToSF("OMScenario__c", scenario);
        }

            // Post new scenario data
        private string PostScenarioToSF(string table, Object content)
        {

            return sf.Post_Object(table, content);
            //return "a0v9D000000oM0DQAU";
        }


    }
}
