using System;
using MainFramework.TestUtils;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions;
using OMREngineTest.Utils;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace Optimizer.Tests.Smoketests.Specflow.StepDefinitions
{
    [Binding]
    public class SchemaSteps : BaseEngineSteps
    {
        public SchemaSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
        }

        [Then(@"I verify '(.*)' tables for (.*) results per '(.*)'")]
        public void VerifyTablesForResultsPer(string schema, string tests, string testfilePath = "")
        {
            if (tests.ToLower() == "skip")
                return;
            else
            {
                ExpectedResultDic all_tests;
                string db_schema = schema;
                string scenario_id = "xxx";
                string jobname = tests == "all" ? testfilePath : tests;

                string tconfig = _scenarioContext.Get<string>("tconfig");
                JSONUnmarshaller testUnmarshaller = new ScenarioJSONUnmarshaller(_scenarioContext, tconfig);
                testUnmarshaller.SetTestId(GetTestIdFromContext());
                TestDefinition tf = testUnmarshaller.GetTests(testfilePath, null);
                tf.PrepareTests(db_schema);
                all_tests = tf.test_dic;

                bool committing = false;
                bool publish = _scenarioContext.ContainsKey("ocesync") ? _scenarioContext.Get<bool>("ocesync") || _scenarioContext.Get<bool>("bulkloaded") || _scenarioContext.Get<bool>("publish") : false;
                string data_check_results = _engine.CheckDataResults(jobname, scenario_id, all_tests, committing, publish, tests);
                bool pass = data_check_results == "OK";
                if (!pass) _output.WriteLine(data_check_results);
                Assert.Equal("OK", data_check_results);
            }
        }
    }
}
