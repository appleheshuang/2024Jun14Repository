using MainFramework;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMREngineTest;
using OMREngineTest.Constructors;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.ScenarioUtils.DataUnmarshallers;
using OMREngineTest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    [Collection("Smoketests")]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]

    public class RCASteps : BaseEngineSteps
    {
        public RCASteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
        }

        [TestPriority(101)]
        [Then(@"I check the root cause of alignments '(.*)'")]
        public void GivenIRunTheFollowingRCATestInDirectoryTheAboveScenario(string dirPath)
        {
            bool pass = true;
            string failures = "RCA failures...\n"; 
            
            string tconfig = _scenarioContext.Get<string>("tconfig");
            RootCauseJSONUnmarshaller rcaUnmarshaller = new RootCauseJSONUnmarshaller(_scenarioContext, tconfig);
            string[] files = rcaUnmarshaller.GetFilesFromDirPath(dirPath);
            foreach (string filePath in files)
            {
                string testname = new FileAndPath(filePath).file;

                RootCauseApiRequestBody requestBody = rcaUnmarshaller.GetRequestBodyOnly(filePath);
                RootCauseApiResponseBody responseTestBody = rcaUnmarshaller.GetResponseBodyTestsOnly(filePath);

                DateTime dbCreatedDateTime = DateTime.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake("select created \"createdatetime\" from information_schema.tables where table_name = 'OMAccount' and table_schema = 'STATIC'", "createdatetime"));
                DateTime timeNow = DateTime.Now;
                Console.WriteLine("Debugging RCA for intermittent issue in OMR-12645...: Time now:" + timeNow + ". DB time created: " + dbCreatedDateTime);
                if ((timeNow - dbCreatedDateTime).TotalDays < 1)
                {
                    Console.WriteLine("\t DB is less than 1 day old -> Replace BeforeEffective with {*N*} in expected response");
                    ReplaceExpectedBeforeEffectiveDatesIfNewDb(responseTestBody, dbCreatedDateTime, timeNow);
                }
                else
                {
                    DateTime effectiveCalcDate = timeNow;
                    string ruleId = _engine.snowq.ReturnSingleColumnValueFromSnowFlake("select \"SOMRuleId\" from output.\"OMAccountTerritoryRule\" where \"OMAccountId\" = '" + requestBody.OMAccountId + "' and \"SOMTerritoryId\" = '" + requestBody.SOMTerritoryId + "' order by \"SOMRuleId\" desc limit 1", "SOMRuleId");
                    string getCalcDate = "select \"EffectiveCalculationDate\" from output.\"OMAccountTerritoryRule\" where \"OMAccountId\" = '" + requestBody.OMAccountId + "' and \"SOMTerritoryId\" = '" + requestBody.SOMTerritoryId + "' and \"SOMRuleId\" = '" + ruleId + "'";
                    try
                    {
                        effectiveCalcDate = (ruleId == "0") ? timeNow : DateTime.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(getCalcDate, "EffectiveCalculationDate"));
                    }
                    catch (Exception e)
                    {
                        _engine.WarningMessage("Error deriving effectiveCalcDate. \nDEBUG: RuleId=" + ruleId + "; Query="+ getCalcDate);
                        throw e;
                    }
                    Console.WriteLine("\t DB is more than 1 day old -> Using effective calculation date: " + effectiveCalcDate);
                    TimeTravelOneDayIfOldDb(responseTestBody, effectiveCalcDate);
                }

                var rcaResponse = _engine.RCA(requestBody);
                _scenarioContext["rcaResponse"] = rcaResponse;

                bool this_test_pass = JsonConvert.SerializeObject(responseTestBody) == JsonConvert.SerializeObject(rcaResponse);
                pass = pass && this_test_pass;
                failures += this_test_pass ? "" : RCAFailureInfo(JsonConvert.SerializeObject(responseTestBody), JsonConvert.SerializeObject(rcaResponse), testname);
            }
            Assert.True(pass, failures);       
        }

        private string RCAFailureInfo(string exp, string act, string testname = "")
        {
            if (testname.Length > 0)
                testname = "[" + testname + "] ";
            return "\t" + testname + " exp=" + exp + "\n\t" + testname + " act=" + act + "\n";
        }

        public void ReplaceExpectedBeforeEffectiveDatesIfNewDb(RootCauseApiResponseBody rcaJobResponse, DateTime dbCreatedDateTime, DateTime timeNow)
        {
            TimeSpan timeDiff = timeNow - dbCreatedDateTime;
            Console.WriteLine("Debugging RCA for intermittent issue in OMR-12645...: timespan difference = " + timeDiff + " which is " + timeDiff.TotalDays + " days");
            if ((timeNow - dbCreatedDateTime).TotalDays < 1)
            {
                foreach (RootCauseRule rules in rcaJobResponse.rules)
                {
                    foreach (RootCauseData data in rules.data)
                    {
                        //If the expected data has an empty string, or expects {*R*} or {*Z*}, or if the table is dated (RuleVersion) then leave it as it is
                        if (!(data.beforeEffective == "" || data.beforeEffective == "{*R*}" || data.beforeEffective == "{*Z*}" || data.table == "OMRuleVersion"))
                        {
                            data.beforeEffective = "{*N*}";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Time travels back one day and replaces BeforeEffective on the test response to {*R*} if it is not {*Z*} or {*N*}
        /// </summary>
        /// <param name="testResponseBody">The unmarshalled RCA response object. Replaces BeforeEffective on the rules if the conditions are met</param>
        /// <param name="effectiveCalcDate">Effective calculation date of the OMAccountTerritoryRule specified by the OMAccountId and SOMTerritoryId of the test body JSON</param>
        public void TimeTravelOneDayIfOldDb(RootCauseApiResponseBody testResponseBody, DateTime effectiveCalcDate)
        {
            //numberOfRowsReturnedWhenTimeTravelToDayBeforeCalculationDate returns number of rows in OMAccountAddress exactly one day before the effective calculation date.
            int numberOfRowsReturnedWhenTimeTravelToDayBeforeCalculationDate = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake("select count(*) \"count\" from static.\"OMAccountAddress\" at (timestamp => (select dateadd(day, -1, '" + effectiveCalcDate + "')))", "count"));
            if (numberOfRowsReturnedWhenTimeTravelToDayBeforeCalculationDate == 0)
            {
                foreach (RootCauseRule rules in testResponseBody.rules)
                {
                    foreach (RootCauseData data in rules.data)
                    {
                        //If the expected data has an empty string, or expects {*R*} or {*Z*} then leave it as it is
                        if (!(data.beforeEffective == "" || data.beforeEffective == "{*N*}" || data.beforeEffective == "{*Z*}"))
                        {
                            data.beforeEffective = "{*R*}";
                        }
                    }
                }
            }
        }
    }
}
