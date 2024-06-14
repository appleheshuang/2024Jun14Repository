using TechTalk.SpecFlow;
using Xunit.Abstractions;
using Newtonsoft.Json.Linq;
using OMREngineTest.ScenarioUtils;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    public class MergeSteps : BaseEngineSteps
    {

        public MergeSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
            _odata = _engine.odataq;
        }

        /// <summary>
        /// Submit the account merge 
        /// Check the response status and body
        /// </summary>
        /// <param name="mergeFile">File contain the merge body</param>
        [When(@"I merge accounts as '(.*)'")]
        public void WhenIMergeAccountsAs(string mergeFile)
        {
            JArray jBody = new MergeUnmarshaller(_scenarioContext, _engine.tenantconfigfile, testid: TestId).GetMergeBody(mergeFile);
            var responseBody = _odata.AccountMerge(jBody);
            string failureMessage = "Response does not match request. Exp count=" + jBody.Count + ", Act count=" + responseBody.Count
                + "\nExp first=" + jBody[0].ToString() + "\n Act first=" + responseBody[0].ToString();
            JobStatusCheck(JToken.DeepEquals(jBody, responseBody), failureMessage);
        }

    }
}
