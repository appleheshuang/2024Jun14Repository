using MainFramework.TestUtils;
using OMREngineTest.Constructors;
using OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.ScenarioUtils.Jobs;
using OMREngineTest.Utils;
using System.Collections.Generic;
using System.Net;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using MainFramework.GlobalUtils;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]

    public class AdjudicationSteps : BaseEngineSteps
    {
        private AdjudicationUnmarshaller adjudicationUnmarshaller;
        private string tconfig;
        Adjudication adjudication;
        List<string> territoryList;
        List<AdjudicationLevel> levels;
        List<AdjudicationChangeRequestAccount> accountCRs;
        List<AdjudicationChangeRequestGeography> geographyCRs;

        public AdjudicationSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
            tconfig = scenarioContext.Get<string>("tconfig");
            adjudicationUnmarshaller = new AdjudicationUnmarshaller(scenarioContext, tconfig, TestId, testdir: TestDataRoot);
            accountCRs = new List<AdjudicationChangeRequestAccount>();
            geographyCRs = new List<AdjudicationChangeRequestGeography>();
        }

        /// <summary>
        /// Ensure AdjudicationSteps is initialized with a new AdjudicationUnmarshaller
        /// This initializes the adjudication users so they can be accessed by other methods
        /// </summary>
        [Given(@"the org is setup for adjudication")]
        public void GivenTheOrgIsSetupForAdjudication()
        {
        }

        [Given(@"the current adjudication defined in '(.*)'")]
        public void GivenTheAdjudicationDefinedAsIsCurrent(string filePath)
        {
            GivenTheAdjudicationJsonInTheDirectory(filePath, false);
        }

        [Given(@"the scenario based adjudication defined in '(.*)'")]
        public void GivenTheScenarioBasedAdjudicationIn(string filePath)
        {
            GivenTheAdjudicationJsonInTheDirectory(filePath, true);
        }

        [TestPriority(101)]
        [Given(@"the adjudication defined as '(.*)' is scenario based '(.*)'")]
        public void GivenTheAdjudicationJsonInTheDirectory(string filePath, bool scenarioBased)
        {
            //bool isAdjudicationScenarioBased = _scenarioContext.ContainsKey("scenario");
            adjudication = adjudicationUnmarshaller.PostAdjudicationObjectsFromFile(filePath, scenarioBased);
            List<AdjudicationTerritory> adjudicationTerritories = adjudicationUnmarshaller.PostAdjudicationTerritoriesFromFile(filePath);
            territoryList = adjudicationTerritories.Select(x => x.SOMTerritoryId__c).ToList();
            levels = adjudicationUnmarshaller.PostAdjudicationLevelsFromFile(filePath);
            adjudicationUnmarshaller.PostAdjudicationGuardRailsFromFile(filePath);

            _scenarioContext["adjudication"] = adjudication;
            _scenarioContext["adjudicationTerritories"] = adjudicationTerritories;
            _scenarioContext["adjudicationLevels"] = levels;
   //         if (scenarioBased)
   //         {
   //             Scenario adjudication_scenario = _scenarioContext.Get<Scenario>("Scenario");
   //             _scenarioContext["tests"] = adjudicationUnmarshaller.GetTests(filePath, adjudication_scenario);
   //         }
        }

        [TestPriority(101)]
        [When(@"I release the adjudication with request body in '(.*)'")]
        public void WhenISubmitTheAdjudicationUseRassignmentForTheAboveData(string userAssignmentBodyFile)
        {
            AdjudicationApiRequestBody userAssignmentBody = adjudicationUnmarshaller.GetRequestBodyFromFile(userAssignmentBodyFile);
            Adjudication adjudication = _scenarioContext.Get<Adjudication>("adjudication");
            adjudicationUnmarshaller.ReleaseAdjudication(adjudication); //Update front end status to RELEASED
            ReleaseAdjudication(adjudication, userAssignmentBody); //Release from backend side
        }
        [When(@"I release the adjudication")]
        public void WhenIReleaseTheAdjudication()
        {
            Adjudication adjudication = _scenarioContext.Get<Adjudication>("adjudication");
            AdjudicationApiRequestBody userAssignmentBody = new AdjudicationApiRequestBody();
            userAssignmentBody.OMAdjudicationId = adjudication.Id;
            userAssignmentBody.OMScenarioId = adjudication.OMScenarioId__c;
            userAssignmentBody.Territories = territoryList;
            userAssignmentBody.EffectiveDate = adjudication.AdjudicationEffectivedate__c;
            userAssignmentBody.SimulationFor = "ALL";
            userAssignmentBody.SOMEngagementPlanId = adjudication.SOMEngagementPlanId__c;
            userAssignmentBody.AdjudicationLevel = levels;
            ReleaseAdjudication(adjudication, userAssignmentBody); //Release from backend side
            adjudicationUnmarshaller.ReleaseAdjudication(adjudication); //Update front end status to RELEASED
        }

        [TestPriority(101)]
        [When(@"I simulate the adjudication with request body in '(.*)'")]
        public void WhenISimulateTheAdjudicationWithRequestBodyIn(string filePath)
        {
            AdjudicationApiRequestBody simulateBody = adjudicationUnmarshaller.GetRequestBodyFromFile(filePath);
            Adjudication adjudication = _scenarioContext.Get<Adjudication>("adjudication");
            SimulateAdjudication(adjudication, simulateBody);
        }
        [When(@"as the (.*) I simulate the adjudication")]
        public void WhenISimulateTheAdjudication(string roleDescr)
        {
            AdjudicationApiRequestBody simulateBody = new AdjudicationApiRequestBody();
            simulateBody.OMAdjudicationId = adjudication.Id;
            simulateBody.OMScenarioId = adjudication.OMScenarioId__c;
            simulateBody.SOMEngagementPlanId = adjudication.SOMEngagementPlanId__c;
            simulateBody.EffectiveDate = adjudication.AdjudicationEffectivedate__c;
            simulateBody.SkipStatusUpdate = false;
            simulateBody.Territories = territoryList;
            simulateBody.SimulationFor = UserRole(roleDescr);
            simulateBody.PullChangeRequests = false;
            simulateBody.AccountChangeRequests = accountCRs.ConvertAll(x => new AccountChangeRequest
            {
                OMAccountId = x.OMAccountId__c,
                Action = x.Action__c,
                SOMTerritoryId = x.SOMTerritoryId__c,
                SOMEngagementSegmentId = x.SOMEngagementSegmentId__c,
                Targets = x.Targets__c
            });
            simulateBody.GeographyChangeRequests = geographyCRs.ConvertAll(x => new GeographyChangeRequest
            {
                SOMGeographyId = x.SOMGeographyId__c,
                Action = x.Action__c,
                SOMTerritoryId = x.SOMTerritoryId__c
            });
            SimulateAdjudication(adjudication, simulateBody);
        }
        [When(@"as the (.*) I simulate the adjudication with PullChangeRequests")]
        public void WhenISimulateTheAdjudicationWithPull(string roleDescr)
        {
            AdjudicationApiRequestBody simulateBody = new AdjudicationApiRequestBody();
            simulateBody.OMAdjudicationId = adjudication.Id;
            simulateBody.OMScenarioId = adjudication.OMScenarioId__c;
            simulateBody.SOMEngagementPlanId = adjudication.SOMEngagementPlanId__c;
            simulateBody.EffectiveDate = adjudication.AdjudicationEffectivedate__c;
            simulateBody.SkipStatusUpdate = false;
            simulateBody.Territories = territoryList;
            simulateBody.SimulationFor = UserRole(roleDescr);
            simulateBody.PullChangeRequests = true;
            SimulateAdjudication(adjudication, simulateBody);
        }

        /// <summary>
        /// Translate the role description into a role code
        /// </summary>
        /// <param name="roleDescr"></param>
        private string UserRole(string roleDescr) {
            if (roleDescr.ToUpper().StartsWith("REQ"))
                return "REQ";
            else if (roleDescr.ToUpper().StartsWith("APPR"))
                return "APPR";
            else if (roleDescr.ToUpper().StartsWith("HO") || roleDescr.ToUpper().StartsWith("HEAD"))
                return "HO";
            else
                throw new System.Exception("Unexpected simulator role: " + roleDescr);
        }

        [Given(@"I assume the (.*) identity")]
        public void GivenIAssumeTheRepIdentity(adj_user_type user_role)
        {
            string token = adjudicationUnmarshaller.AssumeIdentity(user_role);
            _scenarioContext.Set<string>(token, "accesstoken");
        }

        [TestPriority(101)]
        [Given(@"the change requests '(.*)'")]
        public void GivenISubmitTheAdjudicationChangeRequestFromFile(string filePath)
        {
            List<AdjudicationChangeRequestAccount> new_accountCRs = adjudicationUnmarshaller.PostAdjudicationAccountChangeRequestsFromFilePath(filePath);
            accountCRs.AddRange(new_accountCRs);
            List<AdjudicationChangeRequestGeography> new_geoCRs = adjudicationUnmarshaller.PostAdjudicationGeographyChangeRequestsFromFilePath(filePath);
            geographyCRs.AddRange(new_geoCRs);
        }

        [TestPriority(101)]
        [When(@"I submit the change requests to be approved")]
        public void WhenISubmitTheChangeRequests()
        {
            foreach (AdjudicationChangeRequestAccount accountCR in accountCRs)
            {
                adjudicationUnmarshaller.SubmitChangeRequestAccount(accountCR);
            }
            foreach (AdjudicationChangeRequestGeography adjCR in geographyCRs)
            {
                adjudicationUnmarshaller.SubmitChangeRequestGeography(adjCR);
            }
        }

        [When(@"as the (.*) I submit the change requests")]
        public void WhenISubmitTheChangeRequestsWithApproval(string roleDescr)
        {
            string approval_status = UserRole(roleDescr) == "REQ" ? "PEND" : "APPR";
            foreach (AdjudicationChangeRequestAccount accountCR in accountCRs)
            {
                adjudicationUnmarshaller.SubmitChangeRequestAccount(accountCR, approval_status);
            }
            foreach (AdjudicationChangeRequestGeography adjCR in geographyCRs)
            {
                adjudicationUnmarshaller.SubmitChangeRequestGeography(adjCR, approval_status);
            }
        }

        [TestPriority(101)]
        [When(@"I approve the (.*) change requests")]
        public void WhenIApproveTheAccountChangeRequest(string cr_type)
        {
            switch (cr_type.ToLower()) { 
                case "geography":
                foreach (AdjudicationChangeRequestGeography cr in geographyCRs)
                {
                    adjudicationUnmarshaller.AdjudicationApproveGeoChangeRequest(cr);
                } 
                    break;
            default:
                foreach (AdjudicationChangeRequestAccount cr in accountCRs)
                {
                    adjudicationUnmarshaller.AdjudicationApproveAccountChangeRequest(cr);
                }
                break; 
            }
        }

        [TestPriority(101)]
        [When(@"I '(.*)' the account change requests")]
        public void WhenITheAccountChangeRequest(string newStatus)
        {
            foreach (AdjudicationChangeRequestAccount accountCR in accountCRs)
            {
                adjudicationUnmarshaller.PatchApprovalStatusAccountCR(accountCR, newStatus);
            }
        }

        [TestPriority(101)]
        [When(@"I '(.*)' the geography change requests")]
        public void WhenITheGeoChangeRequest(string newStatus)
        {
            foreach (AdjudicationChangeRequestGeography geoCR in geographyCRs)
            {
                adjudicationUnmarshaller.PatchApprovalStatusGeoCR(geoCR, newStatus);
            }
        }

        [TestPriority(101)]
        [When(@"I close the adjudication")]
        public void WhenICloseTheAdjudication()
        {
            Adjudication adjudication = _scenarioContext.Get<Adjudication>("adjudication");
            adjudicationUnmarshaller.CloseAdjudication(adjudication);           
        }

        [When(@"I '(.*)' the adjudication linked scenario")]
        public void WhenITheAdjudicationLinkedScenarioWithConfiguration(string jobType)
        {
            Scenario scenario = _scenarioContext.Get<Scenario>("Scenario");
            Adjudication adjudication = _scenarioContext.Get<Adjudication>("adjudication");
            bool committing = jobType == "commit";

            _scenarioContext["scenarioJobType"] = jobType;
            _scenarioContext["committing"] = committing;
            string execution_type = committing ? "scenarioCommit" : "scenarioCalc";

            string query_tag = "Auto-" + GetTestIdFromContext();
            ScenarioJob adjudicatedScenario = _scenarioContext.Get<ScenarioJob>("scenarioJob"); 
            adjudicatedScenario.WithSettings(OptimizerSettings)
                .WithAdjudicationId(adjudication.Id)
                .WithAdjudicationStatus(adjudication.AdjudicationStatus__c)
                .WithRegion(scenario.SOMRegionId__c)
                .BuildRequestBody();
            adjudicatedScenario.SubmitJob(execution_type);

            int calc_time_sec = EngineTest.max_scenario_calculation_time + EngineTest.start_processing_timeout_for_nonempty_queue;
            int sync_time_sec = EngineTest.max_scenario_sync_time;
            JobStatusCheck(adjudicatedScenario.IsSuccessful(calc_time_sec), ScenarioFailMessage(adjudicatedScenario.latest_status, (int)calc_time_sec / 60, scenario.Id, scenario.Name, "IsSuccessful", adjudicatedScenario.GetScenarioErrors(committing)), adjudicatedScenario.api_info);
            if (committing)
                JobStatusCheck(adjudicatedScenario.scenario_syncjob.IsSuccessful(sync_time_sec), ScenarioFailMessage(adjudicatedScenario.scenario_syncjob.latest_status, (int)sync_time_sec / 60, scenario.Id, scenario.Name, "sync"), adjudicatedScenario.api_info);
            
            scenario.ScenarioStatus__c = adjudicatedScenario.ScenarioStatus;
            _scenarioContext["Scenario"] = scenario;
            _scenarioContext["scenarioJob"] = adjudicatedScenario;

        }

        /// <summary>
        /// Process the current adjudication - generate a linked dummy scenario and commit
        /// </summary>
        /// <param name="settings"></param>
        [When(@"process the current adjudication")]
        public void WhenITheCurrentAdjudicationWithConfiguration()
        {
            //Create a dummy scenario based on the adjudiction
            Scenario scenario = adjudicationUnmarshaller.DummyScenarioForCurrentAdjudication(adjudication); 

            string query_tag = "Auto-" + GetTestIdFromContext();
            ScenarioJob adjudicatedScenario = new ScenarioJob(_engine.t, _engine.engine_token, scenario.Id, query_tag);
            adjudicatedScenario.WithSettings(OptimizerSettings)
                .WithAdjudicationId(adjudication.Id)
                .WithAdjudicationStatus(adjudication.AdjudicationStatus__c)
                .WithRegion(scenario.SOMRegionId__c)
                .BuildRequestBody();
            adjudicatedScenario.SubmitJob("scenarioCommit");

            int calc_time_sec = EngineTest.max_scenario_calculation_time + EngineTest.start_processing_timeout_for_nonempty_queue;
            int sync_time_sec = EngineTest.max_scenario_sync_time;
            JobStatusCheck(adjudicatedScenario.IsSuccessful(calc_time_sec), ScenarioFailMessage(adjudicatedScenario.latest_status, (int)calc_time_sec / 60, scenario.Id, scenario.Name, "IsSuccessful", adjudicatedScenario.GetScenarioErrors(true)), adjudicatedScenario.api_info);
            JobStatusCheck(adjudicatedScenario.scenario_syncjob.IsSuccessful(sync_time_sec), ScenarioFailMessage(adjudicatedScenario.scenario_syncjob.latest_status, (int)sync_time_sec / 60, scenario.Id, scenario.Name, "sync"), adjudicatedScenario.api_info);

            _scenarioContext["Scenario"] = scenario;
            _scenarioContext["scenarioJob"] = adjudicatedScenario;
        }

        /// <summary>
        /// Process the adjudication - current or scenario linked
        /// Having closed the adjudicaiton, dynamically determine whether to execute the scenario
        ///  (scenario based adj), or commit the current adjudication
        /// </summary>
        /// <param name="settings"></param>
        [When(@"I (.*) the adjudication results")]
        public void WhenITheAdjudicationWithConfiguration(string executionType)
        {
            if (adjudication.IsCurrent())
                WhenITheCurrentAdjudicationWithConfiguration();
            else
                WhenITheAdjudicationLinkedScenarioWithConfiguration(executionType);
        }

        public void ReleaseAdjudication(Adjudication adjudication, AdjudicationApiRequestBody requestParameters)
        {
            AdjudicationJob adjudicationJob = new AdjudicationJob(_engine.t, _engine.engine_token, adjudication.Id);
            Assert.True(HttpStatusCode.OK == adjudicationJob.SubmitAdjudicationUserAssignmentJob(requestParameters), AdjudicationFailMessage(adjudicationJob, adjudication));
            //Assert.True(adjudicationJob.HasCompleted(5 * 60), "Adjudication user assignment incomplete after 5 min:" + adjudication.Name); //check the OMJob status
            JobStatusCheck(adjudicationJob.IsSuccessful(), AdjudicationFailMessage(adjudicationJob, adjudication), adjudicationJob.api_info);
        }

        public void SimulateAdjudication(Adjudication adjudication, AdjudicationApiRequestBody requestParameters)
        {
            AdjudicationJob adjudicationJob = new AdjudicationJob(_engine.t, _engine.engine_token, adjudication.Id);
            Assert.True(HttpStatusCode.OK == adjudicationJob.SubmitAdjudicationSimulationJob(requestParameters), AdjudicationFailMessage(adjudicationJob, adjudication));
            JobStatusCheck(adjudicationJob.IsSuccessful(), AdjudicationFailMessage(adjudicationJob, adjudication), adjudicationJob.api_info);
            if (adjudicationJob.IsSuccessful())
            {
                foreach (AdjudicationChangeRequestAccount adj in accountCRs)
                    adjudicationUnmarshaller.PatchSimulateStatusAccountCR(adj, "DONE", requestParameters.SimulationFor);
                foreach (AdjudicationChangeRequestGeography adj in geographyCRs)
                    adjudicationUnmarshaller.PatchSimulateStatusGeoCR(adj, "DONE", requestParameters.SimulationFor);
            }
            Pause();
        }

        string ScenarioFailMessage(string act_status, int wait_min, string scenario_id, string scenario_name = "", string subjobtype = "", List<string> errors = null)
        {
            string fail_message = "Scenario " + subjobtype + " status = " + act_status + ", max wait time = " + wait_min + " min, ScenarioId = " + scenario_id + ", \""+ scenario_name + "\"";
            if (errors != null && errors.Count > 0)
                fail_message += "\nScenarioError:\n" + string.Join("\n", errors);
            return fail_message;
        }

        string AdjudicationFailMessage(AdjudicationJob adjudicationJob, Adjudication adjudication)
        {
            return "Adjudication job with AdjudicationId: " + adjudication.Id + " failed with Status = " + adjudicationJob.latest_status + ". JobId = " + adjudicationJob.jobid;
        }
    }
}
