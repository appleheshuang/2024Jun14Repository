using MainFramework.TestUtils;
using MainFramework.GlobalUtils;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.Utils;
using System.Net;
using System;
using System.IO;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using OMREngineTest.ScenarioUtils.Jobs;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    //[Binding]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    public class ScenarioSteps : BaseEngineSteps
    {
        public enum TemporalType { past, current, future };
        public TemporalType timeframe;
        bool skipSfChecks = false;
        ScenarioJob scenariojob;
        String[] scenarioModelIds;

        private struct ScenarioTableRow
        {
            public string Name { get; }
            public string File { get; }
            public string ExpStatus { get; }
            public string JobType { get; }
            public ScenarioJob Job { get; set; }
            public ScenarioTableRow(string name, string file, string exp, string type = "simulate")
            {
                Name = name; File = file; ExpStatus = exp; JobType = type;
                Job = null;
            }
        }

        public ScenarioSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
        }

        /// <summary>
        /// Post the scenario, model and actions to salesforce
        /// Parse the tests
        /// Initialize the scenarioJob
        /// Load the scenario, tests and job into the context for reference across step objects
        /// </summary>
        /// <param name="setname">Name of the scenario</param>
        /// <param name="filePath">Location of the file containing the scenario json and tests</param>
        [TestPriority(101)]
        [Given(@"the scenario test '(.*)' defined by '(.*)'")]
        public void GivenTheScenarioJsonInTheFile(string setname, string filePath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            string sf_accesstoken = _scenarioContext.ContainsKey("accesstoken") ? _scenarioContext.Get<string>("accesstoken") : null;
            ScenarioJSONUnmarshaller scenarioUnmarshaller = new ScenarioJSONUnmarshaller(_scenarioContext, tconfig, sf_accesstoken, TestDataRoot);
            scenarioUnmarshaller.SetTestId(GetTestIdFromContext());
            Scenario scenario = scenarioUnmarshaller.PostScenarioObjectsFromFile(filePath, setname);
            scenarioModelIds = scenarioUnmarshaller.GetModelIds();
            _scenarioContext["Scenario"] = scenario;
            TestDefinition smoketests = scenarioUnmarshaller.GetTests(filePath, scenario);
            _scenarioContext["tests"] = smoketests;
            _scenarioContext["jsonfile"] = filePath;
            timeframe = ScenarioTemporality(scenario.EffectiveDate__c, scenario.EndDate__c);
            string query_tag = "Auto-" + GetTestIdFromContext();
            scenariojob = new ScenarioJob(_engine.t, _engine.engine_token, scenario.Id, query_tag);
            _scenarioContext.Set<ScenarioJob>(scenariojob, "scenarioJob");
        }

        [Given(@"I want to (.*) the model")]
        public void GivenIWantToOperateTheModel(string operation)
        {
            GivenIWantToOperateModelX(operation, 0);
        }
        [Given(@"I want to (.*) model number (.*)")]
        public void GivenIWantToOperateModelX(string operation, int modelNum)
        {
            string _operation = char.ToUpper(operation[0]) + operation[1..].ToLower();
            string _modelId = scenarioModelIds[modelNum];
            scenariojob.WithModel(_modelId, _operation);
        }

        /// <summary>
        /// Having created a scenario, add actions
        /// </summary>
        /// <param name="dirPath">Directory containing scenario json files</param>
        [Given(@"I add to the scenario '(.*)'")]
        public void GivenIAddToTheScenario(string dirPath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            string sf_accesstoken = _scenarioContext.ContainsKey("accesstoken") ? _scenarioContext.Get<string>("accesstoken") : null;
            ScenarioJSONUnmarshaller scenarioUnmarshaller = new ScenarioJSONUnmarshaller(_scenarioContext, tconfig, sf_accesstoken, TestDataRoot);
            scenarioUnmarshaller.SetTestId(GetTestIdFromContext(true));
            scenarioUnmarshaller.AddScenarioObjectsFromDir(dirPath);
        }

        TemporalType ScenarioTemporality(string start, string end)
        {
            DateTime start_date = DateTime.Parse(start);
            DateTime end_date = DateTime.Parse(end+" 23:59:59");
            if (start_date > DateTime.Now)
                return TemporalType.future;
            else if (end_date < DateTime.Now)
                return TemporalType.past;
            else
                return TemporalType.current;
        }

        [Given(@"a unique id for the test")]
        public string GivenAUniqueIdForTheTest()
        {
            string test_uid = GetTestIdFromContext();
            _output.WriteLine("Test Id = " + test_uid);
            return test_uid;
        }
        string FailMessage(string act_status, int wait_min, string scenario_id, string subjobtype = "", List<string> errors = null)
        {
            string fail_message = "Scenario " + subjobtype + " status = " + act_status + ", max wait time = " + wait_min + " min, ScenarioId = " + scenario_id;
            if (errors != null && errors.Count > 0)
                fail_message += "\nScenarioError:\n" + string.Join("\n", errors);
            return fail_message;
        }

        [Given(@"I commit initial data '(.*)'")]
        public void GivenICommitInitialDataWithConfiguration(string filePath)
        {
            string setname = new FileAndPath(filePath).file;
            GivenTheScenarioJsonInTheFile(setname, filePath);
            WhenISubmitTheScenarioWithConfiguration("commit");
            AndTheScenarioIsSuccessful();
        }
        [Given(@"I commit initial data '(.*)' and check results")]
        public void GivenICommitInitialDataWithConfigurationAndCheckResults(string filePath)
        {
            string setname = new FileAndPath(filePath).file;
            GivenTheScenarioJsonInTheFile(setname, filePath);
            WhenITheScenarioWithConfigurationAndCheckResults("commit");
        }
        [Given(@"I commit initial data '(.*)' and publish (.*)")]
        public void GivenICommitInitialDataWithConfigurationAndPublishOther(string filePath, string publish)
        {
            GivenICommitInitialDataWithConfigurationAndCheckResults(filePath);
            if (publish == "all" || publish == "other")
                AndTheScenarioSubjobIsSuccessful("publishOther", EngineTest.max_ocesync_time);
            if (publish == "all" || publish == "alignments")
                AndTheScenarioSubjobIsSuccessful("publishAlignments", EngineTest.max_ocesync_time); 
        }


        /// <summary>
        /// Read the table row into an array of scenarios
        /// Load the rows in accordance with the "Order" column, if provided
        /// Load the job type if provided. Otherwise default to simulate
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private ScenarioTableRow[] ScenarioArray(Table t)
        {
            int i = 0; string jobType = "simulate";
            ScenarioTableRow[] values = new ScenarioTableRow[t.RowCount];
            foreach (var row in t.Rows)
            {
                if (t.ContainsColumn("JobType")) jobType = row["JobType"];
                try
                {
                    if (t.ContainsColumn("Order")) i = int.Parse(row["Order"]);
                    values[i] = new ScenarioTableRow(row["TestName"], row["ScenarioFile"], row["ExpStatus"], jobType);
                }
                catch (Exception e){
                    if (t.ContainsColumn("Order"))
                        throw new Exception("Order value is " + row["Order"] + ". Must be an integer between 0 and rowcount-1", e);
                    else
                        throw e;
                }
                i++;
            }
            return values;
        }

        /// <summary>
        /// Submit the jobs all at once. Wait for them all complete. Then check all the results.
        /// </summary>
        /// <param name="table"></param>
        [When(@"I simulate the scenario and check results from table")]
        public void WhenISimulateTheScenarioAndCheckResultsFromTable(Table table)
        {
            ScenarioTableRow[] scenarios_to_process = ScenarioArray(table);
            List<string> scenarioIds = new List<string> { };
            for (int i=0; i<table.RowCount; i++)
            {
                GivenTheScenarioJsonInTheFile(scenarios_to_process[i].Name, scenarios_to_process[i].File);
                WhenISubmitTheScenarioWithConfiguration(scenarios_to_process[i].JobType);
                scenarios_to_process[i].Job = _scenarioContext.Get<ScenarioJob>("scenarioJob");
                scenarioIds.Add(scenarios_to_process[i].Job.scenario_id);
            }
            WhenTheScenariosHaveCompleted(scenarioIds);
            for (int i = 0; i < table.RowCount; i++)
            {
                scenariojob = scenarios_to_process[i].Job;
                AndTheScenarioHasTheStatus(scenarios_to_process[i].ExpStatus);
                Scenario sc = new Scenario();
                sc.Id = scenarios_to_process[i].Job.scenario_id;
                sc.Name = scenarios_to_process[i].Name;
                _scenarioContext["Scenario"] = sc;
                CheckResults("all", scenarios_to_process[i].File);
            }
        }

        /// <summary>
        /// Submit the jobs in order by the "Order" column. Wait for each job to complete
        /// before beginning the next one.
        /// </summary>
        /// <param name="table"></param>
        [When(@"I run the ordered scenarios and check results from table")]
        public void WhenIRunTheScenarioAndCheckResultsFromTable(Table table)
        {
            ScenarioTableRow[] scenarios_to_process = ScenarioArray(table);
            for (int i = 0; i < table.Rows.Count; i++)
            {
                GivenTheScenarioJsonInTheFile(scenarios_to_process[i].Name, scenarios_to_process[i].File);
                WhenISubmitTheScenarioWithConfiguration(scenarios_to_process[i].JobType);
                AndTheScenarioHasTheStatus(scenarios_to_process[i].ExpStatus);
                CheckResults();
            }
        }

        /// <summary>
        /// Wait till they have all the scenarios in the list have completed
        /// </summary>
        /// <param name="scenario_ids">scenarios</param>
        /// <param name="max_wait_min"></param>
        /// <param name="pause_sec"></param>
        void WhenTheScenariosHaveCompleted(List<string> scenario_ids, int max_wait_min = 60, int pause_sec = 15)
        {
            const string completed_query = "select count(*) as \"NumCompleted\""
                + " from static.\"OMScenario\" where \"Id\" in (scenario_ids)"
                + " and \"ScenarioStatus\" in ('COMTD','CALTD','ERROR','SYNCERROR')";
            string ids = "'" + string.Join("','", scenario_ids) + "'";
            string completed_count = completed_query.Replace("scenario_ids", ids);
            int num_completed = 0; int elapsed = 0;
            while (num_completed < scenario_ids.Count && elapsed < max_wait_min * 60)
            {
                string answer = _engine.snowq.ReturnSingleColumnValueFromSnowFlake(completed_count, "NumCompleted");
                try
                {
                    num_completed = int.Parse(answer);
                }
                catch
                {
                    if (answer == null || answer == "")
                        num_completed = 0;
                    else
                        throw new Exception("Expecting a number; Got " + answer);
                }
                elapsed += pause_sec;
                System.Threading.Thread.Sleep(pause_sec * 1000);
            }
        }

        /// <summary>
        /// Having already loaded a scenario into sfdc, get the scenario from snowflake and 
        /// update put it into the context, as it would be if it had just been posted
        /// </summary>
        /// <param name="sc_name">Scenario name (without Test Id appended)</param>
        [Given(@"the existing scenario named '(.*)'")]
        public void GivenTheExistingScenarioNamed(string sc_name)
        {
            Scenario sc = new Scenario();
            string scenarioNameWithUId = sc_name + "_" + GetTestIdFromContext(must_exist: true);
            scenariojob = new ScenarioJob(_engine.t, _engine.engine_token, null);
            sc.Id = scenariojob.GetInternalScenarioId(scenarioNameWithUId, max_wait: 10);
            Assert.False(sc.Id.Contains("not found"), "Scenario with name '" + scenarioNameWithUId + "' not found.");
            _scenarioContext.Set<Scenario>(sc, "Scenario");
        }


        [TestPriority(101)]
        [When(@"I '(.*)' the scenario and check results")]
        public void WhenITheScenarioWithConfigurationAndCheckResults(string jobType)
        {
            WhenITheScenarioWithConfigurationAndCheckResultsAfterMin(jobType, 0);
        }
        [TestPriority(101)]
        [When(@"I '(.*)' the scenario and check (.*) results")]
        public void WhenITheScenarioWithConfigurationAndQuickCheckResults(string jobType, string testtype)
        {
            WhenISubmitTheScenarioWithConfiguration(jobType);
            if (_scenarioContext.ContainsKey("waitMin"))
                AndTheScenarioIsSuccessfulWithin(_scenarioContext.Get<int>("waitMin"));
            else
                AndTheScenarioIsSuccessful();
            Scenario scenario = _scenarioContext.Get<Scenario>("Scenario");
            ExpectedResultDic all_tests = _scenarioContext.Get<TestDefinition>("tests").test_dic;
            switch (testtype) { 
                case "snow":
                    Assert.Equal("OK", _engine.CheckSnowResults(all_tests));
                    break;
                case "quick":
                    Assert.Equal("OK", _engine.CheckSnowResults(all_tests));
                    break;
                case "odata":
                    Assert.Equal("OK", _engine.CheckOdataResults(all_tests, jobType == "commit", scenario.Id));
                    break;
                case "sf":
                    Assert.Equal("OK", _engine.CheckSfResults(all_tests));
                    break;
                default:                        
                    Assert.Equal("OK", _engine.CheckDataResults(scenario.Name, scenario.Id,all_tests, jobType == "commit", skipSfChecks));
                    break;
            }
        }
        [When(@"I '(.*)' the scenario and check (.*) results within (.*) min")]
        public void WhenITheScenarioWithConfigurationAndQuickCheckResultsAfterMin(string jobType, string testtype, int wait_min)
        {
            _scenarioContext["waitMin"] = wait_min;
            WhenITheScenarioWithConfigurationAndQuickCheckResults(jobType, testtype);
        }

        [TestPriority(101)]
        [When(@"I '(.*)' the scenario and check results within (.*) min")]
        public void WhenITheScenarioWithConfigurationAndCheckResultsAfterMin(string jobType, int wait_min)
        {
            int calc_time_sec = wait_min != 0 ? wait_min * 60 
                : EngineTest.max_scenario_calculation_time + EngineTest.start_processing_timeout_for_nonempty_queue;
            int sync_time_sec = EngineTest.max_scenario_sync_time;

            WhenISubmitTheScenarioWithConfiguration(jobType);

            //Check status
            ScenarioJob this_scenario_job = _scenarioContext.Get<ScenarioJob>("scenarioJob");
            string this_scenario_id = _scenarioContext.Get<Scenario>("Scenario").Id;
            JobStatusCheck(this_scenario_job.HasCompleted(calc_time_sec), FailMessage(this_scenario_job.latest_status, (int)calc_time_sec / 60, this_scenario_id, "HasCompleted"));
            JobStatusCheck(this_scenario_job.IsSuccessful(), FailMessage(this_scenario_job.latest_status, (int)calc_time_sec / 60, this_scenario_id, "IsSuccessful", this_scenario_job.GetScenarioErrors(jobType == "commit")), this_scenario_job.api_info);
            if (jobType == "commit" && !skipSfChecks)
                JobStatusCheck(this_scenario_job.scenario_syncjob.IsSuccessful(sync_time_sec), FailMessage(this_scenario_job.scenario_syncjob.latest_status,(int)sync_time_sec / 60, this_scenario_id, "sync"), this_scenario_job.api_info);

            UpdateScenarioStatusInContext(this_scenario_job.ScenarioStatus);

            //Check data tests, if any
            if (_scenarioContext.ContainsKey("expectedResultsDic"))
                Assert.Equal("OK", _engine.CheckDataResults(_scenarioContext.Get<Scenario>("Scenario").Name, this_scenario_id, _scenarioContext.Get<ExpectedResultDic>("expectedResultsDic"), jobType == "commit"));
        }

        [Given(@"the test is snowflake only")]
        public void GivenTheTestIsSnowflakeOnly()
        {
            skipSfChecks = true;
        }

        [TestPriority(101)]
        [When(@"I '(.*)' the scenario with '(.*)' mode")]
        public void WhenISubmitTheScenarioWithMode(string jobType, string jobMode)
        {
            Scenario scenario = _scenarioContext.Get<Scenario>("Scenario");
            bool committing = jobType == "commit";
            string db_schema = committing ? "OUTPUT" : "SC_" + scenario?.Id?.ToUpper() + "_OUTPUT";
            TestDefinition smoketests = _scenarioContext.ContainsKey("tests") ? _scenarioContext.Get<TestDefinition>("tests") : null;
            if (smoketests != null)
            {
                smoketests.PrepareTests(db_schema);
                _scenarioContext["expectedResultsDic"] = smoketests.test_dic;
            }

            _scenarioContext["committing"] = committing;
            _scenarioContext["scenarioJobType"] = jobType;
            string execution_type = committing ? "scenarioCommit" : "scenarioCalc";

            if (_scenarioContext.ContainsKey("customValidation")) {
                scenariojob.WithSettings(OptimizerSettings)
                    .WithCustomValidation(_scenarioContext.Get<List<CustomValidation>>("customValidation"))
                    .WithRegion(scenario.SOMRegionId__c)
                    .WithMode(jobMode)
                    .BuildRequestBody();
            } else
            {
                scenariojob.WithSettings(OptimizerSettings)
                    .WithRegion(scenario.SOMRegionId__c)
                    .WithMode(jobMode)
                    .BuildRequestBody();
            }
            if (jobType != "skip")
            {
                HttpStatusCode job_status = scenariojob.SubmitJob(execution_type);
                if (job_status != HttpStatusCode.OK) _engine.LogMessage("Logging API request:\n" + scenariojob.api_info);
                Assert.Equal(HttpStatusCode.OK, job_status);
            }
            _scenarioContext["scenarioJob"] = scenariojob;
        }

        [When(@"I '(.*)' the scenario")]
        public void WhenISubmitTheScenarioWithConfiguration(string jobType)
        {
            WhenISubmitTheScenarioWithMode(jobType,"");
        }


        [Given(@"custom validation defined by '(.*)'")]
        public void GivenCustomValidationDefinedBy(string filePath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            ScenarioJSONUnmarshaller scenarioUnmarshaller = new ScenarioJSONUnmarshaller(_scenarioContext, tconfig);
            _scenarioContext.Set<List<CustomValidation>>(scenarioUnmarshaller.GetCustomValidationRequest(filePath), "customValidation");
        }

        [TestPriority(101)]
        [When(@"the scenario has status '(.*)'")]
        public void AndTheScenarioHasTheStatus(string exp_status)
        {
            bool commiting = _scenarioContext.Get<bool>("committing");
            int max_start_delay = _engine.odataq.QueuedJobCount() == 0 ? EngineTest.max_start_processing_delay_with_empty_queue : EngineTest.start_processing_timeout_for_nonempty_queue; //if there are queued jobs recalc can take longer
            int max_calc_time = max_start_delay + EngineTest.max_scenario_calculation_time;
            int max_sync_time = commiting ? EngineTest.max_scenario_sync_time : 0;
            string act_status = scenariojob.WaitForScenario(exp_status, max_calc_time + max_sync_time);
            if (exp_status != act_status)
            //Show info about the failing job
            {
                string msg = FailMessage(act_status, wait_min: decimal.ToInt16((max_calc_time + max_sync_time) / 60), scenario_id: scenariojob.scenario_id, "'" + scenariojob.GetScenarioInfo("Name") + "'", scenariojob.GetScenarioErrors(commiting));
                JobStatusCheck(exp_status == act_status, msg, scenariojob.api_info);
            }
        }
        [Given(@"I get the internal scenario with name like %'(.*)'%")]
        public void GivenIGetTheInternalScenarioWithNameLike(string scenarioName)
        {
            string scenarioNameWithUId = scenarioName.Replace("{{TestUniqueIntegrationId}}", GetTestIdFromContext(must_exist: true));
            scenariojob = new ScenarioJob(_engine.t, _engine.engine_token, null);
            scenariojob.GetInternalScenarioId(scenarioNameWithUId, max_wait:1800);
            Assert.False(scenariojob.scenario_id.Contains("not found"), "Scenario containing name " + scenarioNameWithUId + " not started after 30 minutes.");
            _scenarioContext.Set<ScenarioJob>(scenariojob, "scenarioJob");
        }

        [TestPriority(101)]
        [When(@"the scenario subjob (.*) is successful within (.*) min")]
        public void AndTheScenarioSubjobIsSuccessful(string scenario_subjob_type, int max_time_min)
        {
            bool subjob_is_successful = false;
            string subjob_status = "";
            
            if (scenario_subjob_type.StartsWith("publish") && timeframe == TemporalType.future) // helps us identify bad test step
                throw new Exception("Invalid step: Scenario publish job not run for future dated scenario");
            
            switch (scenario_subjob_type)
            { 
                case "scenarioSync": 
                    subjob_is_successful = scenariojob.scenario_syncjob.IsSuccessful(max_time_min * 60); 
                    subjob_status = scenariojob.scenario_syncjob.latest_status; 
                    break;
                case "publishOther":
                    subjob_is_successful = scenariojob.publish_other_job.IsSuccessful(max_time_min * 60);
                    subjob_status = scenariojob.publish_other_job.latest_status;
                    break;
                case "publishAlignments": 
                    subjob_is_successful = scenariojob.publish_alignments_job.IsSuccessful(max_time_min*60); 
                    subjob_status = scenariojob.publish_alignments_job.latest_status; 
                    break;
            }
            JobStatusCheck(subjob_is_successful, FailMessage(subjob_status, max_time_min, scenariojob.scenario_id, scenario_subjob_type), scenariojob.api_info);
        }

        /// <summary>
        /// Check all results in the test directory
        /// </summary>
        /// <param name="testDirPath">Contains test json files</param>
        [Then(@"check all results in '(.*)'")]
        public void ThenCheckAllResultsIn(string testDirPath)
        {
            if (testDirPath.EndsWith(".json"))
                CheckResults("all", testDirPath);
            else
            {
                string[] files = Directory.GetFiles(new FileHelper().GetDirectoryPath(testDirPath, TestDataRoot));
                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);
                    string filepath = Path.Combine(testDirPath, filename);
                    CheckResults("all",filepath);
                }
            }
        }

        [Then(@"check (.*) results per '(.*)'")]
        public void CheckResults(string test_name = "all", string testfilePath = "")
        {

            if (test_name.ToLower() == "skip")
                return;
            else
            {
                ExpectedResultDic all_tests = PreparedTests(testfilePath);
                string scenario_id; string jobname;
                // Get the scenario & assoc elements
                try
                {
                    Scenario scenario = _scenarioContext.Get<Scenario>("Scenario");
                    scenario_id = scenario.Id;
                    jobname = scenario.Name;
                }
                catch
                {
                    scenario_id = "xxx";
                    jobname = test_name == "all" ? testfilePath : test_name;
                }

                bool committing = _scenarioContext.ContainsKey("committing") ? _scenarioContext.Get<bool>("committing") : false;
                bool publish = _scenarioContext.ContainsKey("ocesync") ? (_scenarioContext.Get<bool>("ocesync") || _scenarioContext.Get<bool>("bulkloaded") || _scenarioContext.Get<bool>("publish")) : false;
                string data_check_results =  _engine.CheckDataResults(jobname, scenario_id, all_tests, committing, publish, test_name);
                bool pass = data_check_results == "OK";
                if (!pass) _output.WriteLine(data_check_results);
                Assert.Equal("OK", data_check_results); 
            }
        }

        /// <summary>
        /// Query snowflake using the browser snowflake token 
        /// </summary>
        /// <param name="testfilePath">File containing the test queries and expected results</param>
        [Then(@"as the browser user I check '(.*)' results per '(.*)'")]
        public void ThenAsTheBrowserUserIQuerySnowflakePer(string test_name = "all", string testfilePath = "")
        {
            String[] namedTests = test_name.Split(new char[]{ ',', ';' });
            SnowflakeApi snowReq;
            if (_scenarioContext.ContainsKey("snowflakeRequester"))
                snowReq = _scenarioContext.Get<SnowflakeApi>("snowflakeRequester");
            else
                snowReq = new SnowflakeApi(_engine.t, _engine.engine_token);
            string data_check_results = _engine.CheckSnowRequests(PreparedTests(testfilePath), snowReq, testlist: namedTests);
            Assert.Equal("OK", data_check_results);
        }

        /// <summary>
        /// Prepare the tests per the json file. Use the already prepared test dic if it's the same
        /// </summary>
        /// <param name="testfilePath"></param>
        /// <returns>Test dictionary for the original tests</returns>
        ExpectedResultDic PreparedTests(string testfilePath = "")
        {
            Scenario scenario; string db_schema;

            string original_json_file = _scenarioContext.ContainsKey("jsonfile") ? _scenarioContext.Get<string>("jsonfile") : "";
            if (original_json_file == testfilePath || testfilePath == "")
                // Use the original jsonfile tests 
                return _scenarioContext.Get<ExpectedResultDic>("expectedResultsDic");
            else
            {
                // Derive the schema from the scenario context, if there is one
                try
                {
                    scenario = _scenarioContext.Get<Scenario>("Scenario");
                    db_schema = _scenarioContext.Get<bool>("committing") ? "OUTPUT" : "SC_" + scenario.Id.ToUpper() + "_OUTPUT";
                }
                catch
                {
                    scenario = null;
                    if (_scenarioContext.ContainsKey("committing"))
                        db_schema = _scenarioContext.Get<bool>("committing") ? "OUTPUT" : "STATIC";
                    else
                        db_schema = "STATIC";
                }
                string tconfig = _scenarioContext.Get<string>("tconfig");
                JSONUnmarshaller testUnmarshaller = new ScenarioJSONUnmarshaller(_scenarioContext, tconfig, testdatapath: TestDataRoot);
                testUnmarshaller.SetTestId(GetTestIdFromContext());
                TestDefinition tf = testUnmarshaller.GetTests(testfilePath, scenario);
                tf.PrepareTests(db_schema);
                return tf.test_dic;
            }
        }

        [Then(@"check results from table")]
        public void CheckResultsfromTable(TechTalk.SpecFlow.Table t)
        {
            foreach (var row in t.Rows)
            {
                string test_name = row[0];
                string testfilePath = row[1];
                CheckResults(test_name, testfilePath);
            }
        }

        [When(@"the scenario is successful within (.*) minutes")]
        public void AndTheScenarioIsSuccessfulWithin(int min)
        {
            JobStatusCheck(scenariojob.IsSuccessful(min * 60), FailMessage(scenariojob.latest_status, min, scenariojob.scenario_id, "IsSuccessful", scenariojob.GetScenarioErrors(_scenarioContext.Get<bool>("committing"))), scenariojob.api_info);
            if (_scenarioContext.Get<bool>("committing"))
            {
                int max_sync_time = EngineTest.max_scenario_sync_time;
                JobStatusCheck(scenariojob.scenario_syncjob.IsSuccessful(max_sync_time), FailMessage(scenariojob.scenario_syncjob.latest_status, (int)max_sync_time / 60, scenariojob.scenario_id, "Sync"), scenariojob.api_info);
            }
            UpdateScenarioStatusInContext(scenariojob.ScenarioStatus);
        }
        [TestPriority(101)]
        [When(@"the scenario is successful")]
        public void AndTheScenarioIsSuccessful()
        {
            int max_start_delay = _engine.odataq.QueuedJobCount() == 0 ? EngineTest.max_start_processing_delay_with_empty_queue : EngineTest.start_processing_timeout_for_nonempty_queue; //if there are queued jobs recalc can take longer
            int max_calc_time = _scenarioContext.Get<bool>("committing") ? EngineTest.max_commit_time : max_start_delay + EngineTest.max_scenario_calculation_time;
            AndTheScenarioIsSuccessfulWithin((int)max_calc_time / 60);
        }
        [TestPriority(101)]
        [When(@"the scenario fails")]
        public void AndTheScenarioFails()
        {
            int max_start_delay = _engine.odataq.QueuedJobCount() == 0 ? EngineTest.max_start_processing_delay_with_empty_queue : EngineTest.start_processing_timeout_for_nonempty_queue; //if there are queued jobs recalc can take longer
            int max_calc_time = max_start_delay + EngineTest.max_scenario_calculation_time;
            JobStatusCheck(scenariojob.HasFailed(max_calc_time), FailMessage(scenariojob.latest_status,(int)max_calc_time / 60, scenariojob.scenario_id, "HasFailed"));
            UpdateScenarioStatusInContext(scenariojob.ScenarioStatus);
        }

        /// <summary>
        /// UpdateScenarioStatusInContext                    
        /// Added for OMR-17976 - Patch resets the scenario to pending, 
        ///  which removes the existing ScenarioChangeRequests
        /// </summary>
        /// <param name="status"></param>
        public void UpdateScenarioStatusInContext(string status)
        {
            if (_scenarioContext.ContainsKey("Scenario"))
            {
                Scenario scenario = _scenarioContext.Get<Scenario>("Scenario");
                scenario.ScenarioStatus__c = status;
                _scenarioContext.Set<Scenario>(scenario, "Scenario"); 
            }
        }

        [TestPriority(101)]
        [When(@"I delete scenarios that are (.*) days old")]
        public void DeleteOldScenarios(int days_old)
        {
            int age = days_old - 1; // since it will be < 00:00 of that day
            string old_scenario_query = "select \"Id\" from static.\"OMScenario\" where \"LastModifiedDate\" < Current_date - " + age
                + " and \"ScenarioStatus\" in ('CALTD','ERROR','PEND') and (\"Type\" = 'USER' or \"Type\" is null);";
            List<string> deleted_scenarios = _engine.snowq.ReturnWholeStringColumnValueFromSnowFlake(old_scenario_query, "Id");
            ScenarioJob scenario_to_delete = new ScenarioJob(_engine.t, _engine.engine_token, "unknown");
            foreach (string scenario_id in deleted_scenarios)
            {
                scenario_to_delete.scenario_id = scenario_id;
                scenario_to_delete.WithSettings(null)
                    .BuildRequestBody();
                Assert.Equal(HttpStatusCode.OK, scenario_to_delete.SubmitJob("scenarioDelete"));

            }
            _scenarioContext["deleted_scenarios"] = deleted_scenarios;
            if (deleted_scenarios.Count > 0)
                scenariojob = scenario_to_delete;
            _scenarioContext["scenarioJob"] = scenariojob;
        }
        [TestPriority(101)]
        [When(@"the scenario is deleted within (.*) minutes")]
        public void ScenariosAreDeleted(string maxwaittime_min)
        {
            if (_scenarioContext.Get<List<string>>("deleted_scenarios").Count > 0)
               ScenarioStatus("DELTD", maxwaittime_min);
        }

        [TestPriority(101)]
        [When(@"the scenario is '(.*)' within (.*) minutes")]
        public void ScenarioStatus(string exp_status, string maxwaittime_min="standard")
        {
            string act_status = "unknown";
            if (maxwaittime_min.ToLower() == "standard" || maxwaittime_min.ToLower() == "std")
            {
                act_status = scenariojob.WaitForScenario(exp_status);
            }
            else
            {
                try
                {
                    int maxwaittime_sec = Int32.Parse(maxwaittime_min) * 60;
                    act_status = scenariojob.WaitForScenario(exp_status, maxwaittime_sec);
                }
                catch (FormatException)
                {
                    _output.LogMessage("Unable to parse "+ maxwaittime_min + "as int");
                }
            }
            JobStatusCheck(exp_status.Contains(act_status), FailMessage(act_status, Int32.Parse(maxwaittime_min), scenariojob.scenario_id, "does not have exp status (" + exp_status + ")"), scenariojob.api_info);
            UpdateScenarioStatusInContext(act_status);
        }
        [TestPriority(101)]
        [When(@"the scenario is '(.*)'")]
        public void ScenarioStatus(string exp_status)
        {
            string act_status = scenariojob.WaitForScenario(exp_status);
            Assert.Equal(exp_status, act_status);
            if (exp_status != act_status)
                LogRequest(scenariojob.api_info);
        }
        [TestPriority(101)]
        [Then(@"purged scenario schemas are removed")]
        public void ScenarioSchemaCheck()
        {
            if (_scenarioContext.Get<List<string>>("deleted_scenarios").Count > 0)
            {
                // Just check 1 of them
                ScenarioJob deleted_scenario = _scenarioContext.Get<ScenarioJob>("scenarioJob");
                Assert.False(deleted_scenario.ScenarioSchemaExists("INPUT"), "Input schema exists for purged scenario " + deleted_scenario.scenario_id);
                Assert.False(deleted_scenario.ScenarioSchemaExists("OUTPUT"), "Output schema exists for purged scenario " + deleted_scenario.scenario_id);
            }
        }
        [Then(@"the committed scenario schemas are removed")]
        public void CommittedScenarioSchemasAreRemoved()
        {
            ScenarioJob committed_scenario = _scenarioContext.Get<ScenarioJob>("scenarioJob");
            Assert.False(committed_scenario.ScenarioSchemaExists("INPUT"), "Input schema exists for committed scenario " + committed_scenario.scenario_id);
            Assert.False(committed_scenario.ScenarioSchemaExists("OUTPUT"), "Output schema exists for committed scenario " + committed_scenario.scenario_id);
        }

    }
}
