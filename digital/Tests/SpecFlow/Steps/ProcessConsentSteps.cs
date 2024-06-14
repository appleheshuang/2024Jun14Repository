using Digital.DigitalUtils.Jobs;
using OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace Digital.Tests.SpecFlow.Steps
{
    [Binding]
    public class ProcessConsentSteps : BaseEngineSteps
    {
        public ProcessConsentSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        { }

        [When(@"trigger a job to process Consents")]
        public void WhenTriggerAJobToProcessConsents()
        {
            //DigitalJob digitalJob = new DigitalJob(_engine.t, _engine.engine_token);
            //Tuple<IRestResponse, String> response = digitalJob.SubmitProcessConsentJob();
            //Assert.True(HttpStatusCode.OK == response.Item1.StatusCode);
        }

    }
}
