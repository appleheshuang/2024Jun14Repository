using OMREngineTest.ScenarioUtils.DataUnmarshallers.OCESyncParameterUnmarshallers;
using Xunit;
using TechTalk.SpecFlow;
using Xunit.Abstractions;
using TechTalk.SpecFlow.UnitTestProvider;
using System.Threading;
using System.Diagnostics;
using OMREngineTest.ScenarioUtils.Jobs;


namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    public class OCESyncSteps : BaseEngineSteps
    {
        private OCESyncParameterUnmarshaller _oceUnmarshaller;
        private readonly IUnitTestRuntimeProvider _unitTestRuntimeProvider;
        private const string initialOceAccounts = "Smoketest\\LoadData\\InitialOceAccounts.json";

        public OCESyncSteps(ScenarioContext scenarioContext, IUnitTestRuntimeProvider unitTestRuntimeProvider, ITestOutputHelper output) : base(scenarioContext, output)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            _oceUnmarshaller = new OCESyncParameterUnmarshaller(_scenarioContext, tconfig);
            _unitTestRuntimeProvider = unitTestRuntimeProvider;
        }

        [When(@"I (.*) to-from OCE-P with '(.*)' mode")]
        public void WhenIOCESyncWithParametersFrom(string synctype, string mode)
        {
            mode = mode == "default" ? _engine.t.mode : mode;
            if (_engine.t.OceMode)
            {
                //OCESyncJobRequestBody parameters = _oceUnmarshaller.GetOCESyncJobParameters(filePath);
                OCESyncJobRequestBody parameters = new OCESyncJobRequestBody(synctype, mode, OptimizerSettings, _engine.t.Parameters);
                 _engine.OCESync(parameters, wait_min:20);
                _scenarioContext["ocesync"] = true;
            }
            else
                _unitTestRuntimeProvider.TestIgnore("\t Tenant mode="+_engine.t.mode+" - skipping OCE Sync");

        }
        [When(@"I (.*) to-from OCE-P")]
        public void WhenISyncTo_FromOCE_PWithParameters(string synctype)
        {
            WhenIOCESyncWithParametersFrom(synctype,"default");
        }
        [Given(@"I resync accounts from OCE-P")]
        public void WhenIResyncAccounts()
        {
            if (_engine.RunDataTests(_scenarioContext, initialOceAccounts, suppress_logging: true) != "OK")
                if (_engine.t.OceMode)
                {
                    OCESyncJobRequestBody parameters = new OCESyncJobRequestBody("PULL_ACCOUNT", _engine.t.mode, "", _engine.t.Parameters, "2020-01-01T00:00:00.0Z");
                    _engine.OCESync(parameters, wait_min: 20);
                    _scenarioContext["ocesync"] = true;
                }
                else
                    _unitTestRuntimeProvider.TestIgnore("\t Tenant mode=" + _engine.t.mode + " - skipping OCE Resync");
        }

        [When(@"I expire records in '(.*)' that are (.*) days old")]
        public void WhenIUpdateTheRecordStatus(string tablename, int days_old)
        {
            string update_query = "update output.\"" + tablename + "\" set \"EndDate\" = Current_date - 1"
               + " where \"CreatedDate\" < Current_date - " + days_old + ";";
            _engine.snowq.RunQuery(update_query);
        }

        [When(@"I end geography alignments for '(.*)' that are (.*) days old")]
        public void WhenIEndGeographyTerritory(string som_id, int days_old)
        {
            string som_ids = "'" + string.Join("','", som_id.Split(',')) + "'";
            string update_query = "update output.\"OMGeographyTerritory\" set \"EndDate\" = Current_date - 1"
               + " where \"CreatedDate\" < Current_date - " + days_old 
               + " and \"SOMGeographyId\" in (" + som_ids + ");";
            _engine.snowq.RunQuery(update_query);
        }
        [When(@"I end geography alignments for '(.*)' that are (.*) hours old")]
        public void WhenIEndGeographyTerritoryByHour(string som_id, int hours_old)
        {
            string som_ids = "'" + string.Join("','", som_id.Split(',')) + "'";
            // End the ones that start prior to today and still active
            string update_query = "update output.\"OMGeographyTerritory\" set \"EndDate\" = Current_date - 1"
               + " where \"CreatedDate\" < timeadd(hour, -" + hours_old + ", Current_timestamp)"
               + " and \"EffectiveDate\" <= Current_date - 1 and \"EndDate\" > Current_date - 1"
               + " and \"Status\" = 'ACTV'"
               + " and \"SOMGeographyId\" in (" + som_ids + ");";
            _engine.snowq.RunQuery(update_query);
            // Inactivate the ones that start today
            update_query = "update output.\"OMGeographyTerritory\" set \"Status\" = 'INAC'"
               + " where \"CreatedDate\" < timeadd(hour, -" + hours_old + ", Current_timestamp)"
               + " and \"EffectiveDate\" = Current_date"
               + " and \"Status\" = 'ACTV'"
               + " and \"SOMGeographyId\" in (" + som_ids + ");";
            _engine.snowq.RunQuery(update_query);
        }

        [Then(@"the max number of geography alignments is (.*)")]
        public void ThenNumberOfGeographyAlignmentsIsLessThan(int max_count)
        {
            string max_query = "select zeroifnull(max(actv_count)) \"actv_count\" from ("
                + " select \"SOMGeographyId\", count(*) actv_count from output.\"OMGeographyTerritory\""
                + " where \"EffectiveDate\" < current_date and \"EndDate\" > current_date - 1"
                + " and \"Status\" = 'ACTV' group by \"SOMGeographyId\");";
            int active_geo_affils = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(max_query, "actv_count"));
            Assert.True(active_geo_affils <= max_count, "Highest number of geo affiliations exceeds allowed. Max allowed=" + max_count + ", Actual=" + active_geo_affils);
        }

        [Given(@"I offset EffectiveDate of new explicits from OCEP for accounts '(.*)' to (.*) days")]
        public void WhenIoffsetAccountExplicit(string account_uids, int days)
        {
            string offSetDays = days.ToString();
            if (days > 0)
            {
                offSetDays = "+" + days.ToString();
            }    

            string list_of_accounts = "'" + string.Join("','", account_uids.Split(',')) + "'";
            string update_query = "update output.\"OMAccountExplicit\" set \"EffectiveDate\" = Current_date " + offSetDays
               + " where \"EffectiveDate\" > current_date - 1"
               + " and \"OMAccountId\" in ( select \"Id\" from static.\"OMAccount\""
               + " where \"UniqueIntegrationId\" in (" + list_of_accounts + ") )"
               + " and \"FromOCEPersonal\" = true;";

            var actualresult =_engine.snowq.RunQuery(update_query);
            while (!actualresult)
            {
                actualresult = _engine.snowq.RunQuery(update_query);
            }
            string checkCount = "select count(*) count from \"INFORMATION_SCHEMA\".\"SCHEMATA\" where \"SCHEMA_NAME\" = 'OUTPUT_PRECLONE'";
            int checkCountResult = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(checkCount, "COUNT"));
            if (checkCountResult != 0)
            
            {
                string update_output_preclone = update_query.Replace("output", "output_preclone");
                _engine.snowq.RunQuery(update_output_preclone);
            }
        }
    }
}
