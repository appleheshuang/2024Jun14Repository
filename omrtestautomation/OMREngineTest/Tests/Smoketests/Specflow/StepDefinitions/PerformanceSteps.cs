using System.Linq;
using MainFramework.GlobalUtils;
using OMREngineTest.ScenarioUtils;
using System;
using System.Threading;
using System.Data;
using System.IO;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using OMREngineTest.ScenarioUtils.Jobs;
using OMREngineTest.ScenarioUtils.DataUnmarshallers;
using OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions;

namespace OMREngineTest.ScenarioUtils
{

    public struct ScenarioConfig
    {
        public Scenario scenario_object;
        public ScenarioRunParams run_settings;
        public string Id => scenario_object.Id;
        public ScenarioConfig(Scenario sc, ScenarioRunParams config)
        {
            scenario_object = sc;
            run_settings = config;
        }
    }
    public struct StaticDataMap
    {
        public Dictionary<string, dynamic> savedDataMap;
        public Dictionary<string, int> savedDataCounts;
        public void Save(ScenarioContext s)
        {
            savedDataMap = new Dictionary<string, dynamic>(s.Get<Dictionary<string, dynamic>>("dynamicDataMap"));
            savedDataCounts = new Dictionary<string, int>(s.Get<Dictionary<string, int>>("dynamicDataCounts"));
        }
        public void Initialize()
        {
            savedDataMap = new Dictionary<string, dynamic>();
            savedDataCounts = new Dictionary<string, int>();
        }
    }
}
namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    public class PerformanceSteps : ScenarioSteps
    {
        private List<ScenarioConfig> scenarios;
        RecalcJob recalc; 
        private List<string> scenario_ids;
        private string recalc_starttime;
        private int iterations;
        private SnowFlakeHelper loader;
        private const string resultsRootDir = "EngineTestResults";
        private const string defaultResultsDir = "Performance";
        private const string recalc_name = "RecalcPerf";

        private StaticDataMap staticData;

        private execution_type submit_method = execution_type.sequential;
        private bool concurrent_submit
        {
            get { return submit_method == execution_type.concurrent; }
        }

        public PerformanceSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
            scenarios = new List<ScenarioConfig> { };
            loader = new SnowFlakeHelper();
            staticData = new StaticDataMap();
            staticData.Initialize();
            string query_tag = "Perf-" + GetTestIdFromContext();
            recalc = new RecalcJob(_engine.t, _engine.engine_token, query_tag);
        }

        public enum collection_type { raw, summary };
        public enum execution_type { sequential, concurrent };

        const int post_execution_wait_sec = 60;

        const string timings_query = "select \"Id\",\"Name\",\"Type\""
             +",\"ProcessingStartTime\",\"CalculationStartTime\",\"ProcessingEndTime\",\"SyncStartTime\",\"SyncEndTime\""
            + ",timediff(time_units, \"ProcessingStartTime\", \"CalculationStartTime\") as \"Merge\""
            + ",timediff(time_units, \"CalculationStartTime\", \"ProcessingEndTime\") as \"RulesCalc\""
            + ",timediff(time_units, \"ProcessingEndTime\", \"SyncStartTime\") as \"SyncQueue\""
            + ",timediff(time_units, \"SyncStartTime\", \"SyncEndTime\") as \"Sync\""
            + ",case when \"SyncEndTime\" is null then timediff(sec,\"ProcessingStartTime\",\"ProcessingEndTime\") else timediff(sec,\"ProcessingStartTime\",\"SyncEndTime\") end as \"Total\""
            + ",substr(\"Name\", 0, position('_' in \"Name\") - 1) as \"CommonName\", \"ScenarioStatus\""
            + " from static.\"OMScenario\" where \"Id\" in (scenario_ids)";
        const string timings_query_avg = "select \"CommonName\" as \"Name\""
            + ", avg(\"Merge\") as \"Merge\", avg(\"RulesCalc\") as \"RulesCalc\""
            + ", avg(\"SyncQueue\") as \"SyncQueue\", avg(\"Sync\") as \"Sync\", avg(\"Total\") as \"Total\""
            + ", count(*) as \"Executions\""
            + " from (timings_query) group by \"CommonName\"";

        [Then(@"I collect (.*) scenario timings in (.*)")]
        public void ThenICollectResultsTo(collection_type query_type, string units)
        {
            ThenICollectResultsTo(query_type, units, defaultResultsDir);
        }
        [Then(@"I collect (.*) timing in sec")]
        public void ThenICollectJobTimingsInSec(string jobname)
        {
            if (_scenarioContext.ContainsKey(jobname))
            {
                int runtime;
                switch (jobname)
                {
                    case "bulkJob":
                        runtime = _scenarioContext.Get<BulkLoadJob>(jobname).GetJobTiming(); break;
                    default:
                        runtime = _scenarioContext.Get<OptimizerJob>(jobname).GetJobTiming(); break;
                }
                _engine.LogMessage(jobname + " run time (sec): " + runtime);
            }
            else
                _engine.LogMessage(jobname + " not found. Timing not collected");
        }

        [Then(@"I collect (.*) scenario timings in (.*) to '(.*)'")]
        public void ThenICollectResultsTo(collection_type query_type, string units, string dirpath)
        {
            string query = timings_query;
            switch (query_type) { 
                case collection_type.summary:
                    query = timings_query_avg.Replace("timings_query", timings_query);
                    break;
            }
            int records_retrieved = RecordScenarioTimings(query_type.ToString(), units, query, dirpath);
            Assert.True(records_retrieved > 0, "No timings recorded for scenarios: " + string.Join(",", from x in scenarios select x.Id));
        }

        private int RecordScenarioTimings(string resulttype, string units, string query, string results_dir)
        {
            // Determine the file path and create the directory
            string test_outline = _scenarioContext.ScenarioInfo.Title;
            string env_version = _engine.tenant.Config.targetEnv + "_" + _engine.tenant.Config.targetVersion;
            string file_name = env_version + "_" + test_outline + "_" + submit_method + "_" + resulttype + "_" + DateTime.Now.ToString("yyyyMMddhh24mmss")+ ".csv";
            string dir_path = new FileHelper().GetDirectoryPath(results_dir,resultsRootDir);
            if (!Directory.Exists(dir_path)) Directory.CreateDirectory(dir_path);
            
            // Get the results and write to the file
            string ids = "'" + string.Join("','", scenario_ids) + "'";
            string timings_query = query.Replace("scenario_ids", ids).Replace("time_units",units);
            IDataReader result = loader.CONN_EXEC_ToSnowflake(_engine.t.snowConnection, timings_query);
            return loader.LOAD_ReaderToCSV(result, Path.Combine(dir_path, file_name), ",");
        }

        [Given(@"I setup the scenario performance tests found in '(.*)' to run (.*) times")]
        public void GivenThePerformanceJsonInTheDirectory(string dirPath, int iterations)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            ScenarioJSONUnmarshaller scenarioUnmarshaller = new ScenarioJSONUnmarshaller(_scenarioContext, tconfig);
            for (int i = 0; i < iterations; i++)
            {
                scenarios.AddRange(scenarioUnmarshaller.PostScenarioObjectsFromDir(dirPath, staticData));
            }
        }

        [Given(@"I save these to the static data map")]
        public void GivenISaveTheseToTheStaticDataMap()
        {
            staticData.Save(_scenarioContext);
        }


        [Given(@"execution is (.*)")]
        public void GivenExecutionIs(execution_type submit)
        {
            submit_method = submit;
        }

        [When(@"I execute scenario performance tests")]
        public void WhenIExecuteScenarioPerformanceTests()
        {
            // submit the scenarios
            foreach (ScenarioConfig sc in scenarios)
            {
                _scenarioContext["Scenario"] = sc.scenario_object;
                WhenISubmitTheScenarioWithConfiguration(sc.run_settings.jobtype);
                if (!concurrent_submit)
                {
                    // if sequential wait till completion + post_execution_wait_sec (for OUTPUT_PRECLONE to regenrate) before submitting the next one
                    AndTheScenarioIsSuccessful();
                    Thread.Sleep(post_execution_wait_sec * 1000);
                }
            }
        }

        [When(@"I execute recalcs (.*) times")]
        public void WhenIExecuteRecalcsTimes(int count)
        {
            iterations = count;
            // submit each recalcs
            for (int i = 0; i < iterations; i++)
            {
                if (concurrent_submit)
                    recalc.SubmitRecalc();
                else
                {
                    recalc.SubmitRecalc();
                    Assert.True(recalc.HasDequeued(), "Recalc job has not been dequeued " + recalc.jobid + ")");
                    // if sequential wait till completion
                    Assert.True(recalc.static_syncjob.IsSuccessful(), "Recalc StaticSync IsSuccessful check failed, Act status=" + recalc.static_syncjob.latest_status + "(jobid:" + recalc.static_syncjob.jobid + ")");
                    Assert.True(recalc.IsSuccessful(), "Recalc IsSuccessful check failed, Act status=" + recalc.latest_status + "(jobid:" + recalc.jobid + ")");
                    Assert.True(recalc.scenario_syncjob.IsSuccessful(), "Recalc Sync IsSuccessful check failed, Act status=" + recalc.scenario_syncjob.latest_status + "(jobid:" + recalc.scenario_syncjob.jobid + ")");
                    // wait after completion (for OUTPUT_PRECLONE to regenrate) before submitting the next one
                    Thread.Sleep(post_execution_wait_sec * 1000);
                }
                if (i == 0) recalc_starttime = recalc.StartTime();
            }
        }

        List<string> GetScenarioIds()
        {
            const string unknown_str = "{not found}";
            List<string> ids = scenarios.Select(i => i.Id.ToString()).ToList();
            while (ids.Contains(unknown_str))
                ids.Remove(unknown_str);
            return ids;
        }
        List<string> GetRecalcIds()
        {
            const int max_wait_min = 60;
            const int pause_sec = 15;

            int elapsed = 0;
            
            List<string> ids = recalc.NextRecalcJobs(recalc_starttime, iterations);
            while (ids.Count < iterations && elapsed < max_wait_min * 60)
            {
                Thread.Sleep(pause_sec * 1000);
                elapsed += pause_sec;
                ids = recalc.NextRecalcJobs(recalc_starttime, iterations);
            }
            return ids;
        }
        [When(@"the scenarios have completed")]
        public void WhenTheScenariosHaveCompleted()
        {
            scenario_ids = GetScenarioIds();
            scenario_ids.AddRange(GetRecalcIds());
            
            if (concurrent_submit)
            {
                // wait till they have all completed
                const string completed_query = "select count(*) as \"NumCompleted\""
                 + " from static.\"OMScenario\" where \"Id\" in (scenario_ids)"
                 + " and \"ScenarioStatus\" in ('COMTD','CALTD','ERROR','SYNCERROR')";

                const int max_wait_min = 60;
                const int pause_sec = 15;
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
                    Thread.Sleep(pause_sec * 1000);
                }
            }
        }
    }
}
