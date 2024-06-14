using MainFramework;
using MainFramework.TestUtils;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.ScenarioUtils.DataUnmarshallers;
using OMREngineTest.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    [Collection("Smoketests")]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    public class DigitalSteps : BaseEngineSteps
    {
        public DigitalSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
        }

        [TestPriority(102)]
        [Given(@"I post Digital record to table from Table")]
        public void GivenIPostTheDigitalObjectsFromTable(TechTalk.SpecFlow.Table t)
        {
            foreach (var row in t.Rows)
            {
                string tablename = row[0];
                string filePath = row[1];
                GivenIPostTheDigitalObjectsFromFile(tablename, filePath);
            }
        }

        [TestPriority(101)]
        [Given(@"I post Digital record to table '(.*)' from file '(.*)'")]
        public void GivenIPostTheDigitalObjectsFromFile(string tablename, string filePath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            string uid = GetTestIdFromContext();
            DigitalUnmarshaller _DigitalUnmarshaller = new DigitalUnmarshaller(_scenarioContext, tconfig);
            _DigitalUnmarshaller.SetTestId(uid);
          List<Object> objects = _DigitalUnmarshaller.PostDigitalObjectsFromFilePath(tablename, filePath, "post");
            _scenarioContext[tablename + "s"] = objects;
        }

        [TestPriority(101)]
        [Given(@"Wait 15 mins")]
        public void Wait15mins()
        {
            Thread.Sleep(840000);
            //Thread.Sleep(60000);
        }

        [Then(@"check (.*) results per '(.*)'")]
        public void CheckResults(string test_name = "all", string testfilePath = "")
        {

            if (test_name.ToLower() == "skip")
                return;
            else
            {
                string original_json_file;
                ExpectedResultDic all_tests;
                Scenario scenario; string db_schema; string scenario_id; string jobname;
                // Get the original jsonfile
                original_json_file = _scenarioContext.ContainsKey("jsonfile") ? _scenarioContext.Get<string>("jsonfile") : "";
                // Get the scenario & assoc elements
                try
                {
                    scenario = _scenarioContext.Get<Scenario>("Scenario");
                    db_schema = _scenarioContext.Get<bool>("committing") ? "OUTPUT" : "SC_" + scenario.Id.ToUpper() + "_OUTPUT";
                    scenario_id = scenario.Id;
                    jobname = scenario.Name;
                }
                catch
                {
                    scenario = null;
                    if (_scenarioContext.ContainsKey("committing"))
                        db_schema = _scenarioContext.Get<bool>("committing") ? "OUTPUT" : "STATIC";
                    else
                        db_schema = "STATIC";
                    scenario_id = "xxx";
                    jobname = test_name == "all" ? testfilePath : test_name;
                }

                if (original_json_file != testfilePath && testfilePath != "")
                {
                    string tconfig = _scenarioContext.Get<string>("tconfig");
                    //JSONUnmarshaller testUnmarshaller = new ScenarioJSONUnmarshaller(_scenarioContext, tconfig, testdatapath: testDataRoot);
                    //testUnmarshaller.SetTestId(GetTestIdFromContext());
                    //TestDefinition tf = testUnmarshaller.GetTests(testfilePath, scenario);

                    DigitalUnmarshaller testUnmarshaller = new DigitalUnmarshaller(_scenarioContext, tconfig);
                    testUnmarshaller.SetTestId(GetTestIdFromContext());
                    TestDefinition tf = testUnmarshaller.GetTests(testfilePath, scenario);
                    tf.PrepareTests(db_schema);
                    all_tests = tf.test_dic;
                }
                else
                    all_tests = _scenarioContext.Get<ExpectedResultDic>("expectedResultsDic");
                bool committing = _scenarioContext.ContainsKey("committing") ? _scenarioContext.Get<bool>("committing") : false;
                bool publish = _scenarioContext.ContainsKey("ocesync") ? (_scenarioContext.Get<bool>("ocesync") || _scenarioContext.Get<bool>("bulkloaded") || _scenarioContext.Get<bool>("publish")) : false;
                string data_check_results = _engine.CheckDataResults(jobname, scenario_id, all_tests, committing, publish, test_name);
                bool pass = data_check_results == "OK";
                if (!pass) _output.WriteLine(data_check_results);
                Assert.Equal("OK", data_check_results);
            }
        }

        [Then(@"check digital results from table")]
        public void CheckTestResultsfromTable(TechTalk.SpecFlow.Table t)
        {
            foreach (var row in t.Rows)
            {
                string test_name = row[0];
                string testfilePath = row[1];
                CheckResults(test_name, testfilePath);
            }
        }


        public class RecordNotFoundException : Exception
        {
            public RecordNotFoundException() { }
            public RecordNotFoundException(string message) : base(message) { }
            public RecordNotFoundException(string message, Exception inner) : base(message, inner) { }
            protected RecordNotFoundException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }
}
