using OMREngineTest.ScenarioUtils.DataUnmarshallers.OCESyncParameterUnmarshallers;
using OMREngineTest.Utils;
using Xunit;
using System;
using TechTalk.SpecFlow;
using Xunit.Abstractions;
using TechTalk.SpecFlow.UnitTestProvider;
using Optimizer.Constructors;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    public class DataSyncSteps : BaseEngineSteps
    {

        public DataSyncSteps(ScenarioContext scenarioContext, IUnitTestRuntimeProvider unitTestRuntimeProvider, ITestOutputHelper output) : base(scenarioContext, output) { }

        [When(@"I trigger a (.*) DataSync '(.*)' with parameter body defined by '(.*)'")]
        public void WhenITriggerADatASyncJobWithParameterBodyDefinedByFile(string exp_status, string jobType, string filePath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");

            switch (exp_status) 
            {
                case "successful":
                    exp_status = "job_success";
                    break;
                case "error":
                    exp_status = "job_error";
                    break;
            }
                    
            DataSyncUnmarshaller unmarshaller = new DataSyncUnmarshaller(_scenarioContext, tconfig);
            DataSyncApiRequestBody requestBody = unmarshaller.GetDataSyncRequestBody(filePath);

            Tuple<bool, string> sync_success = _datasync.DataSync(requestBody, exp_status, wait_min: (int)EngineTest.max_scenario_sync_time/60);

            if (!sync_success.Item1)
                LogRequest(_datasync.dataSyncJob.api_info);
            Assert.True(_datasync.dataSyncJob.IsSuccessful(), "DataSync IsSuccessful check failed, Act status=" + _datasync.dataSyncJob.latest_status + "(jobid:" + _datasync.dataSyncJob.jobid + ")");
        }
    }
}
