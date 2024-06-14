using System;
using MainFramework.GlobalUtils;
using OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace Optimizer.Tests.Smoketests.Specflow.StepDefinitions
{
    [Binding]
    public class StaticSyncSteps : BaseEngineSteps
    {
        public StaticSyncSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
            _snowq = new QuerySnowflake(_engine.t.snowConnection);
        }

        [Given(@"I insert a record to snowflake OMSalesForce table")]
        public void GivenIInsertARecordToSnowflakeOMSalesForceTable()
        {
            string id = _scenarioContext.Get<string>("TestUniqueIntegrationId");
            string insertQuery = string.Format("insert into \"OUTPUT\".\"OMSalesForce\" \r\n(\"SOMSalesForceId\",\"Name\",\"Type\",\"SOMRegionId\",\"UniqueIntegrationId\",\"Status\",\"EffectiveDate\",\"EndDate\",\"CreatedDate\",\"LastModifiedDate\") " +
                "values('ReSyncTest_{0}','ReSyncSF_{0}','INST','REG01','ReSyncTest_{0}','ACTV','2023-01-01','3999-12-31',Current_Date,Current_Date);", id);
            _engine.snowq.RunQuery(insertQuery);
            string query = string.Format("select count(*) \"recs\" from \"OUTPUT\".\"OMSalesForce\" where \"UniqueIntegrationId\"='ReSyncTest_{0}';", id);
            string recCount = _engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs");
            Assert.Equal("1", recCount);
        }

        [Given(@"I insert a record to snowflake OMProduct table")]
        public void GivenIInsertARecordToSnowflakeOMProductTable()
        {
            string id = _scenarioContext.Get<string>("TestUniqueIntegrationId");
            string insertQuery = string.Format("insert into \"STATIC\".\"OMProduct\" (\"SOMProductId\",\"Name\",\"Type\",\"SOMRegionId\",\"UniqueIntegrationId\",\"Status\",\"EffectiveDate\",\"EndDate\",\"CreatedDate\",\"LastModifiedDate\") " +
                "values ('SFProduct_{0}','SFProduct_{0}','Detail','REG01','SFProduct_{0}','ACTV','2023-01-01','3999-12-31',Current_Date,Current_Date);", id);
            _engine.snowq.RunQuery(insertQuery);
            string query = string.Format("select count(*) \"recs\" from \"STATIC\".\"OMProduct\" where \"UniqueIntegrationId\"='SFProduct_{0}';", id);
            string recCount = _engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs");
            Assert.Equal("1", recCount);
        }

        [Given(@"I delete row with Id '([^']*)' from '([^']*)' schema '([^']*)' table from Snowflake")]
        public void GivenIDeleteRowWithIdFromSchemaTableFromSnowflake(string id, string schema, string table)
        {
            string recordId = id + _scenarioContext.Get<string>("TestUniqueIntegrationId");
            _snowq.DeleteRecord(table, schema, recordId);
        }

        [When(@"I run static sync with Resync true")]
        public void WhenIStaticSyncWithResyncTrue()
        {
            string sync_result = _engine.SubmitStaticSync(true);
            Assert.True(sync_result == "OK", sync_result);
        }

        [When(@"I run static sync")]
        public void WhenIRunStaticSync()
        {
            string sync_result = _engine.SubmitStaticSync();
            Assert.True(sync_result == "OK", sync_result);
        }

    }
}
