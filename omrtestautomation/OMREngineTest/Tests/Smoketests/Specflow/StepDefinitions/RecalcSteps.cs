using MainFramework.TestUtils;
using OMREngineTest.Utils;
using System;
using System.Net;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.ScenarioUtils.Jobs;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    [Collection("Smoketests")]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    public class RecalcSteps : BaseEngineSteps
    {
        public RecalcSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
        }

        [TestPriority(102)]
        [When(@"I '(.*)' the account '(.*)' and run a recalc")]
        public void WhenIPatchTheAccountStatusAndRunARecalc(string new_status, string acct_id)
        {
            ThenIUpdateEntityWithUniqueId(new_status, "OMAccount", acct_id);
            ThenIRunARecalcWithParameters("Test Automation - Recalc with "+ new_status + " account");
        }

        [TestPriority(102)]
        [When(@"I dataprotect the account '(.*)' and run a recalc")]
        public void WhenIDataProtectTheAccountAndRunARecalc(string acct_id)
        {
            ThenIDataProtectTheAccountWithUniqueId(acct_id);
            ThenIRunARecalcWithParameters("Test Automation - Recalc with dataprotection");
        }

        [TestPriority(102)]
        [Then(@"I run a recalc")]
        public void ThenIRunARecalc()
        {
            ThenIRunARecalcWithParameters("Test Automation - Recalc");
        }

        string FailMessage(string act_status, int wait_min, string recalc_id, string subjobtype = "")
        {
            return "Recalc " + subjobtype + " status = " + act_status + ", max wait time = " + wait_min + " min, Id = " + recalc_id;
        }

        [TestPriority(102)]
        [Then(@"I run the recalc '(.*)'")]
        public void ThenIRunARecalcWithParameters(string recalc_desc)
        {
            // Submit the recalc
            string query_tag = "Auto-" + GetTestIdFromContext();
            RecalcJob recalc = new RecalcJob(_engine.t, _engine.engine_token, query_tag);
            HttpStatusCode recalcresponse = recalc.SubmitRecalc(OptimizerSettings);
            Assert.True(recalcresponse == HttpStatusCode.OK, "Submit Recalc failed: " + recalcresponse);

            //Make sure the recalc has been dequeued before processing further
            JobStatusCheck(recalc.HasDequeued(1800), recalc.jobid + " failed to dequeue after 30 minutes possibly due to timeout. Please retry or check job status.", recalc.api_info);

            // Add the recalc details to the scenario context, so it can be accessed by tests
            Recalc recalc_details = new Recalc(recalc.scenario_id, recalc_desc, recalc_desc);
            recalc_details.SetFieldsToUniqueIntegrationId(recalc_desc, GetTestIdFromContext());
            _scenarioContext["Scenario"] = recalc_details;
            _scenarioContext["committing"] = true;
            JSONUnmarshaller recalcData = new ScenarioJSONUnmarshaller(_scenarioContext);
            recalcData.AddToDataMapSingleObject(recalc_details, "recalcCount");

            //Check it recalc and assoc jobs complete successfully - static sync, recalc, sync
            int max_start_delay = _engine.odataq.QueuedJobCount() == 0 ? EngineTest.max_start_processing_delay_with_empty_queue : EngineTest.max_commit_time; //if there are queued jobs recalc can take longer
            int max_calc_time = max_start_delay + EngineTest.max_commit_time;
            int max_sync_time = EngineTest.max_scenario_sync_time;
            JobStatusCheck(recalc.static_syncjob.HasCompleted(max_calc_time), FailMessage(recalc.static_syncjob.latest_status, max_calc_time/60, recalc.static_syncjob.jobid,"StaticSync HasCompleted"),recalc.api_info); //check the OMJob status for static sync
            JobStatusCheck(recalc.static_syncjob.IsSuccessful(0), FailMessage(recalc.static_syncjob.latest_status, max_calc_time / 60, recalc.static_syncjob.jobid, "StaticSync IsSuccessful"), recalc.api_info); //check the OMJob status for static sync
            JobStatusCheck(recalc.HasCompleted(max_calc_time), FailMessage(recalc.latest_status, max_calc_time / 60, recalc.jobid, "Recalc HasCompleted"), recalc.api_info); //check the OMJob status for recalc
            JobStatusCheck(recalc.IsSuccessful(0), FailMessage(recalc.latest_status, max_calc_time / 60 ,recalc.jobid, "Recalc IsSuccessful"),recalc.api_info); //check the OMJob status for recalc
            JobStatusCheck(recalc.scenario_syncjob.HasCompleted(max_sync_time), FailMessage(recalc.scenario_syncjob.latest_status, max_sync_time/60, recalc.scenario_syncjob.jobid, "Sync HasCompleted"), recalc.api_info); //check the OMJob status for sync
            JobStatusCheck(recalc.scenario_syncjob.IsSuccessful(0), FailMessage(recalc.scenario_syncjob.latest_status, max_sync_time / 60, recalc.scenario_syncjob.jobid, "Sync IsSuccessful"), recalc.api_info); //check the OMJob status for sync
            JobStatusCheck(recalc.dataProtectionJob.HasCompleted(max_calc_time), FailMessage(recalc.dataProtectionJob.latest_status, max_calc_time / 60, recalc.dataProtectionJob.jobid, "DataProtection HasCompleted"), recalc.api_info); //check the OMJob status for dataprotection
            JobStatusCheck(recalc.dataProtectionJob.IsSuccessful(0), FailMessage(recalc.dataProtectionJob.latest_status, max_calc_time / 60, recalc.dataProtectionJob.jobid, "DataProtection IsSuccessful"), recalc.api_info); //check the OMJob status for dataprotection
            // Update the recalc description for easier tracking/debugging
            _engine.odataq.PatchSingleValue("OMScenario", recalc.scenario_id, "Description", recalc_desc);
        }

        [TestPriority(102)]
        [Then(@"I check the account aligned dates")]
        public void ThenICheckTheAccountsAligned()
        {
            string exp_after = _scenarioContext.Get<string>("BeforeResult").Replace(_scenarioContext.Get<string>("ScenarioEndDate"), DateTime.Parse(_scenarioContext.Get<string>("Today")).AddDays(-1).ToString("yyyy-MM-dd"));
            string after_result = _engine.snowq.GetJsonFormatWithoutUniqkey(_scenarioContext.Get<string>("RecalcQuery") + " limit 1");
            Assert.Equal(exp_after, after_result);
        }

        [Then(@"I '([^']*)' the '([^']*)' with UniqueId '([^']*)'")]
        public void ThenIUpdateEntityWithUniqueId(string new_status, string entity, string entityId)
        {
            string id = entityId.Replace("{{TestUniqueIntegrationId}}", GetTestIdFromContext(must_exist: true));
            HttpStatusCode patchresponce = _engine.odataq.PatchSingleValue(entity, id, "Status", new_status);
            Assert.True(patchresponce == HttpStatusCode.OK, "Patch "+entity+" (" + new_status + ") failed: " + patchresponce);
        }

        [Then(@"I dataprotect the OMAccount with UniqueId '([^']*)'")]
        public void ThenIDataProtectTheAccountWithUniqueId(string entityId)
        {
            string accUid = entityId.Replace("{{TestUniqueIntegrationId}}", GetTestIdFromContext(must_exist: true));

            string acct_status_req = "/OMAccount?$select=Id&$filter=UniqueIntegrationId eq '" + accUid + "'";
            string account_id = _engine.odataq.GetSingleValue("Id", acct_status_req, "STATIC");

            HttpStatusCode patchresponce = _engine.odataq.PatchSingleValue("OMAccount", account_id, "DataProtected", "TRUE");
            Assert.True(patchresponce == HttpStatusCode.OK, "Patch OMAccount" + " (true) failed: " + patchresponce);
        }

    }
}
