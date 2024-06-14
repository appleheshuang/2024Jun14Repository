using Digital.DigitalUtils.Jobs;
using OMREngineTest;
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
    public class DigitalDataSyncSteps : BaseEngineSteps
    {
        public DigitalDataSyncSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        { }

        [When(@"I trigger a DigitalDataSync with Type '([^']*)' and cache '([^']*)'")]
        public void WhenIScheduleAOCEPConnectorJobWithTypeAndJobTypeCache(string type, string cache)
        {
            DigitalDataSyncJob digitalDataSyncJob = new DigitalDataSyncJob(_engine.t, _engine.engine_token);
            digitalDataSyncJob.SubmitScheduleDatasyncCache(type, cache);
            _scenarioContext.Set<DigitalDataSyncJob>(digitalDataSyncJob, "digitalDataSyncJob");
        }

        [When(@"I trigger a DigitalDataSync with Type '([^']*)' and cache '([^']*)' with ExcludedAliases '([^']*)'")]
        public void WhenIScheduleAOCEPConnectorJobWithTypeAndJobTypeCacheExcludedAliases(string type, string cache, string ExcludedAliases)
        {
            DigitalDataSyncJob digitalDataSyncJob = new DigitalDataSyncJob(_engine.t, _engine.engine_token);
            digitalDataSyncJob.SubmitScheduleDatasyncCacheExcludedAliases(type, cache, ExcludedAliases);
            _scenarioContext.Set<DigitalDataSyncJob>(digitalDataSyncJob, "digitalDataSyncJob");
        }


        [When(@"I trigger a ResetInitialSync with '([^']*)' and '([^']*)'")]
        public void WhenIResetInitialSyncForSingleObject(string OMTableName, string DEExternalKey)
        {
            DigitalDataSyncJob digitalDataSyncJob = new DigitalDataSyncJob(_engine.t, _engine.engine_token);
            digitalDataSyncJob.ResetInitialSyncSingleObject(OMTableName, DEExternalKey);

        }

        [Then(@"check all DigitalDataSync jobs finished successfully")]
        public void ThenAllScheduledOCEPConnectorJobFinishSuccessfully()
        {
            DigitalDataSyncJob digitalDataSyncJob = _scenarioContext.Get<DigitalDataSyncJob>("digitalDataSyncJob");
            JobStatusCheck(digitalDataSyncJob.IsAllSuccessful(), "Queued job with JobId : " + digitalDataSyncJob.jobid + " failed.", digitalDataSyncJob.api_info); //check the OMJob status for recalc
        }

        [Then(@"check all DigitalDataSync jobs finished")]
        public void ThenAllScheduledOCEPConnectorJobFinish()
        {
            DigitalDataSyncJob digitalDataSyncJob = _scenarioContext.Get<DigitalDataSyncJob>("digitalDataSyncJob");
            JobStatusCheck(digitalDataSyncJob.IsAllComplete(), "Queued job with JobId : " + digitalDataSyncJob.jobid + " failed.", digitalDataSyncJob.api_info); //check the OMJob status for recalc
        }
    }
}
