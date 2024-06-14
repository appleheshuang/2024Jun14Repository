using MainFramework.GlobalUtils;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.Utils;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    public class SnowApiSteps : BaseEngineSteps
    {

        SnowflakeApi snowReq;
        JSONUnmarshaller testQueryUnmarshaller;
        string schema;
        QueryResult testQuery;

        public SnowApiSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
            testQueryUnmarshaller = new JSONUnmarshaller(scenarioContext);
            schema = "OUTPUT";
            if (_scenarioContext.ContainsKey("snowflakeRequester"))
                snowReq = _scenarioContext.Get<SnowflakeApi>("snowflakeRequester");
            else
                snowReq = new SnowflakeApi(_engine.t, _engine.engine_token);
        }

        [Given(@"the default schema '(.*)'")]
        public void GivenTheDefaultSchema(string dfltSchema)
        {
            schema = dfltSchema;
        }

        [Then(@"I set the snowflake token")]
        public void ThenIGetASnowflakeToken()
        {
            _scenarioContext.Set<SnowflakeApi>(snowReq, "snowflakeRequester");
        }

        /// <summary>
        /// Post the request from the named test in the given file
        /// to the snowflake Rest API
        /// </summary>
        /// <param name="testname">Name of the snowflake test to run</param>
        /// <param name="testfile">Relative path of the file containing the test</param>
        [When(@"I issue the snowflake request '(.*)' per '(.*)'")]
        public void WhenIIssueTheSnowflakeRequest(string testname, string testfile)
        {
            TestDefinition tester = testQueryUnmarshaller.GetTests(testfile);
            tester.PrepareTests(schema);
            testQuery = tester.test_dic.GetTest(testname);
            snowReq.SubmitQuery(testQuery.GetQuery(schema));
        }

        /// <summary>
        /// Check the status code of the response
        /// </summary>
        /// <param name="expStatusCode">String representing the response status code</param>
        [Then(@"the request status is '(.*)'")]
        public void ThenTheRequestStatusIs(string expStatusCode)
        {
            Assert.Equal(expStatusCode,snowReq.StatusCode);
        }

        /// <summary>
        /// Check that the key-value pair provided is present in the response
        /// </summary>
        /// <param name="key">Label of the response element</param>
        /// <param name="value">Expected value</param>
        [Then(@"the response '(.*)' matches '(.*)'")]
        public void ThenTheResponseBodyContains(string key,string value)
        {
            Assert.Equal(value, snowReq.GetResult(key));
        }

        [Then(@"the response '(.*)' contains '(.*)'")]
        public void ThenTheResponseContains(string key, string value)
        {
            Assert.Contains(value, snowReq.GetResult(key));
        }

        /// <summary>
        /// Check that the parsed response matches the expected result 
        /// from the named test in the given file
        /// Requires the step
        /// </summary>
        [Then(@"I check the response data")]
        public void ThenICheckTheResponseData()
        {
            Assert.Equal(testQuery.GetResult(), snowReq.GetResult("data"));
        }

    }
}
