using MainFramework;
using MainFramework.TestUtils;
using OMREngineTest.ScenarioUtils.DataUnmarshallers;
using OMREngineTest.ScenarioUtils.Jobs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using OMREngineTest.Utils;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    [Collection("Smoketests")]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    public class SalesforceSteps : BaseEngineSteps
    {
        public SalesforceSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
        }

        private const int max_waittime = 600;
        private const int dflt_wait_interval = 5;


        [TestPriority(101)]
        [Given(@"I post the salesforce users in file '(.*)'")]
        public void GivenIPostTheSalesforceUsersInFile(string filePath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");

            SalesforceUnmarshaller userUnmarshaller = new SalesforceUnmarshaller(_scenarioContext, tconfig);
            List<User> users = userUnmarshaller.PostUsersFromFilePath(filePath); //Post salesforce object and trigger STATICSYNC

            //Make sure static sync finishes syncing these users before doing anything else
            int elapsed_time = 0;
            int intWait = 10;
            foreach (User user in users)
            {
                string query = "select count(*) \"recs\" from STATIC.\"OMUser\" where \"SOMUserId\" = '" + user.SOMUserId__c + "';";
                int recCount = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs"));
                while (recCount == 0 && elapsed_time <= 240)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(intWait));
                    elapsed_time += intWait;
                    recCount = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs"));
                }
                if (recCount == 0) throw new RecordNotFoundException("OMUser","SOMUserId", user.SOMUserId__c, "Static Sync OMUser");
            }
            _scenarioContext["users"] = users;
        }

        [Given(@"I wait for '(.*)' identified by '(.*)' to static sync")]
        public void ThenIWaitForIdentifiedByToStaticSync(string object_name, string key_column)
        {
            string sf_keycolumn = key_column + (key_column.EndsWith("__c") ? "" : "__c");
            string snow_keycol = key_column.Replace("__c", "");
            string snow_table = (object_name.StartsWith("OM") ? "" : "OM") + object_name;
            List<string> values = new List<string> { };
            List<Object> objects = _scenarioContext.Get<List<Object>>(object_name + "s");
            foreach (Object obj in objects)
            {
                foreach (var fieldInfo in obj.GetType().GetFields())
                    if (fieldInfo?.Name == sf_keycolumn) values.Add(fieldInfo?.GetValue(obj)?.ToString());
            }
            //List<string> values = objects.Select(o => o.GetType().GetProperty(sf_keycolumn).GetValue(o).ToString());

            int elapsed_time = 0; int intWait = 10;
            int exp_count = values.Count;
            string query = "select count(*) \"recs\" from STATIC.\"" + snow_table + "\" where \"" + snow_keycol + "\" in ('" + string.Join("','", values) + "');";
            int recCount = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs"));
            while (recCount != exp_count && elapsed_time <= max_waittime)
            {
                Thread.Sleep(TimeSpan.FromSeconds(intWait));
                elapsed_time += intWait;
                recCount = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs"));
            }
            if (recCount != exp_count) throw new RecordNotFoundException(snow_table, snow_keycol, string.Join(",", values), "Static Sync");
        }

        /// <summary>
        /// GivenIWaitForTheUpdatesToStaticSyncPerTests
        /// Wait for the snowflake to match the tests in the given file. 
        /// If not matching by end of wait interval, then fail and log the mismatches
        /// </summary>
        /// <param name="testfile"></param>
        [Given(@"I check that updates have static synced per '(.*)'")]
        public void GivenIWaitForTheUpdatesToStaticSyncPerTests(string testfile)
        {
            int elapsed_time = 0; int intWait = dflt_wait_interval;
            ExpectedResultDic expectedResultsDic = _engine.PrepareTests(_scenarioContext, testfile, TestDataRoot);
            while (_engine.CheckSnowResults(expectedResultsDic, suppress_logging: true) != "OK" && elapsed_time <= max_waittime)
            { 
                Thread.Sleep(TimeSpan.FromSeconds(intWait));
                elapsed_time += intWait;
            }
            Assert.Equal("OK", _engine.CheckSnowResults(expectedResultsDic));
        }


        [Then(@"I wait for the table '(.*)' to static sync after the scenario has completed")]
        public void ThenIWaitForTheTablesToStaticSyncAfterTheScenarioHasCompleted(string object_name)
        {
            string snow_table = (object_name.StartsWith("OM") ? "" : "OM") + object_name;

            ScenarioJob scenariojob = _scenarioContext.Get<ScenarioJob>("scenarioJob");
            string syncFinishTime = scenariojob.GetScenarioSyncFinishTime(scenariojob.scenario_id);

            int maxTimeToWait = 30; //maximum wait time of 30 seconds
            int elapsed_time = 0; int intWait = 5;
            int exp_count = 1;
            string query = "select count(*) \"recs\" from STATIC.\"OMJob\" where \"Details\" ilike '%" + snow_table + "%' and \"ProcessingStartTime\" >= '" + syncFinishTime + "' and \"Type\" = 'STATICSYNC' and \"Status\" = 'Success'";

            int recCount = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs"));
            while (recCount < exp_count && elapsed_time <= maxTimeToWait)
            {
                Thread.Sleep(TimeSpan.FromSeconds(intWait));
                elapsed_time += intWait;
                recCount = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs"));
            }
            if (recCount < exp_count) throw new RecordNotFoundException("Not able to find Static Sync completion after scenario " + scenariojob.scenario_id);
        }


        [TestPriority(101)]
        [Given(@"I (.*) (.*) salesforce '(.*)' from file '(.*)' on ""(.*)""")]
        public void GivenIPostTheSalesforceObjectsFromFileWithMatch(string methodType, sf_obj_type type, string object_name, string filePath, string uniqueCol)
        {
            _scenarioContext["ocesync"] = true;
            string tconfig = _scenarioContext.Get<string>("tconfig");
            string uid = GetTestIdFromContext(must_exist: true);
            SalesforceUnmarshaller sfUnmarshaller = new SalesforceUnmarshaller(_scenarioContext, tconfig, TestDataRoot);
            sfUnmarshaller.SetTestId(uid);
            List<Object> objects = sfUnmarshaller.PostSFObjectsFromFilePath(type, object_name, filePath, methodType, UniqueIdCol: uniqueCol); //Post salesforce object
            _scenarioContext[object_name + "s"] = objects;
        }

        [Given(@"I (.*) (.*) salesforce '(.*)' from file '(.*)'")]
        public void GivenIPostTheSalesforceObjectsFromFile(string methodType, sf_obj_type type, string object_name, string filePath)
        {
            GivenIPostTheSalesforceObjectsFromFileWithMatch(methodType, type, object_name, filePath, "UniqueIntegrationId__c");
        }

        /// <summary>
        /// Similar to Bulkload initial for snowflake:
        ///  - First check the sfqueries. If they pass there is no need to load the data. (If they fail
        ///   this can be due to not loaded, or details do not match expected)
        ///  - If the sfqueries fail, post the data to sfdc. If the post fails with a duplicate error,
        ///   patch the data istead.
        /// Expected use: Given initial omr salesforce 'MetricDefinition' from file '<MetricDefinitionsFile>' on "SOMMetricDefinitionId__c"
        /// </summary>
        /// <param name="type">oce, native, omr</param>
        /// <param name="object_name">Object name without namespace. If type is native OM prefix can be left off</param>
        /// <param name="filePath"></param>
        /// <param name="id_column">Unique column to match existing data (if not UniqueIntegrationId)</param>
        [Given(@"initial (.*) salesforce '(.*)' from file '(.*)' on ""(.*)""")]
        public void GivenIPostInitialSalesforceObjectsFromFile(sf_obj_type type, string object_name, string filePath, string id_column)
        {
            if (_engine.RunDataTests(_scenarioContext, filePath, suppress_logging: true, sfquery_check: true) != "OK")
            {
                GivenIPostTheSalesforceObjectsFromFileWithMatch("post", type, object_name, filePath, id_column);
                string tablename = (object_name.StartsWith("OM") ? "" : "OM") + object_name;
                WhenIStaticSyncTable(tablename);
                Assert.Equal("OK", _engine.RunDataTests(_scenarioContext, filePath, sfquery_check: true));
            }
            
        }

        [When(@"I static sync (.*)")]
        public void WhenIStaticSyncTable(string table_name)
        {
            string sync_result = _engine.SubmitStaticSync(tables: table_name);
            Assert.True(sync_result == "OK", sync_result);
        }


        public class RecordNotFoundException : Exception
        {
            public RecordNotFoundException() { }
            public RecordNotFoundException(string table, string column, string value, string context = "Misc") : base(context + ": " + table + " not found for " + column + " = " + value + " after " + max_waittime / 60 + " min") { }
            public RecordNotFoundException(string message) : base(message) { }
            public RecordNotFoundException(string message, Exception inner) : base(message, inner) { }
            protected RecordNotFoundException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        [Given(@"I post the OCE objects in file '(.*)'")]
        public void GivenIPostTheOCEObjectsInFile(string filePath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");

            OCEJSONUnmarshaller oceUnmarshaller = new OCEJSONUnmarshaller(_scenarioContext, tconfig);
            oceUnmarshaller.PostSetupOCEObjectsFromFilePath(filePath); //Post OCE objects (currently will try to do this even if mode is OMR)
        }
    }
}
