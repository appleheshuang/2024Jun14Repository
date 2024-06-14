using System;
using System.Net;
using System.Linq;
using RestSharp;
using System.Collections.Generic;
using MainFramework;
using MainFramework.GlobalUtils;
using System.Threading;

namespace OMREngineTest.ScenarioUtils.Jobs
{

    public class ScenarioJob : OptimizerJob
    {
        public List<string> error_codes;
        public List<string> ignore_error_codes;
        public List<string> completed_statuses;
        public string scenario_id;
        private string adjudicationId;
        private string adjudicationStatus;
        private string settings;
        private string region;
        private string mode;
        private string modelId;
        private string modelOperation;
        private List<CustomValidation> customValidations;
        private string opt_query_tag;

        public ApiRequestBody requestBody;
        private List<string> jobs_that_sync_to_sf = new List<string> { "scenarioCommit", "recalc" };
        private List<string> jobs_that_staticsync = new List<string> { "recalc" };
        private List<string> jobs_that_publish = new List<string> { "scenarioCommit" };
        private List<string> jobs_that_trigger_dataprotection = new List<string> { "recalc" };
        public SyncJob scenario_syncjob;
        public OptimizerJob static_syncjob;
        public OCESyncJob publish_other_job;
        public OCESyncJob publish_alignments_job;
        public DataProtectionJob dataProtectionJob;

        public string sf_client; public string sf_secret; public string sf_user; public string sf_pswd;
        public string sf_url; public string sf_namespace; // for sync purposes

        private string scenario_output_schema { get { return "SC_" + scenario_id.ToUpper() + "_OUTPUT"; } }

        public ScenarioJob(TenantConfig tenant_info, eToken token, string id, string query_tag=null)
            : base(tenant_info, token, id)
        {
            scenario_id = id;
            error_codes = new List<string> { _status_code["error"], _status_code["syncerror"] };
            ignore_error_codes = new List<string> { _status_code["deleted"], _status_code["cancelled"] };
            opt_query_tag = query_tag;
            mode = tenant_info.mode?.ToUpper() ?? "";
        }

        public ScenarioJob WithAdjudicationId(string adjudicationId)
        {
            this.adjudicationId = adjudicationId;
            return this;
        }

        public ScenarioJob WithAdjudicationStatus(string adjudicationStatus)
        {
            this.adjudicationStatus = adjudicationStatus;
            return this;
        }

        public ScenarioJob WithCustomValidation(List<CustomValidation> customValidations)
        {
            this.customValidations = customValidations;
            return this;
        }

        public ScenarioJob WithSettings(string settings)
        {
            this.settings = settings;
            return this;
        }

        public ScenarioJob WithRegion(string region)
        {
            this.region = region;
            return this;
        }

        public ScenarioJob WithMode(string jobMode)
        {
            if (jobMode != "" && jobMode != null)
                mode = jobMode.ToUpper();
            return this;
        }

        public ScenarioJob WithModel(string modelId, string modelOperation)
        {
            this.modelId = modelId;
            this.modelOperation = modelOperation;
            return this;
        }

        public void BuildRequestBody()
        {
            var scenarioRequestBody = (adjudicationId != null && adjudicationStatus != null)
                 ? new ScenarioApiRequestBody(scenario_id, settings, adjudicationId, adjudicationStatus, t.Parameters, mode, region)
                 : new ScenarioApiRequestBody(scenario_id, settings, t.Parameters, mode, region);
            if (customValidations != null)
            {
                scenarioRequestBody.CustomValidation = customValidations;
            }
            if (modelId != null)
            {
                scenarioRequestBody.ModelId = modelId;
                scenarioRequestBody.ModelOperation = modelOperation;
            }
            requestBody = scenarioRequestBody;
        }

        public HttpStatusCode SubmitJob(string type = "scenarioCalc")
        {
            Tuple<IRestResponse, String> resp = null;
            resp = api.Request_RestSharp(type, "POST", requestBody, query_tag:opt_query_tag);
            api_info = api._request;
            if (resp.Item1.StatusCode == HttpStatusCode.Unauthorized) api_info += api._auth;

            if (resp.Item1.StatusCode != HttpStatusCode.OK) {
                Console.WriteLine("Error submitting scenario: " + resp.Item1.StatusCode + "," + resp.Item1.ErrorMessage);
                return resp.Item1.StatusCode;
            }
            try
            {
                string respbody = resp.Item2;
                jobid = api.GetValuefromResponseBody("JobId", respbody, "JobType", _jobtype_code[type]);
                if (jobs_that_sync_to_sf.Contains(type))
                {
                    string sync_job_id = api.GetValuefromResponseBody("JobId", respbody, "JobType", _jobtype_code["scenarioSync"]);
                    scenario_syncjob = new SyncJob(t, engine_token, sync_job_id);
                }
                if (jobs_that_trigger_dataprotection.Contains(type))
                {
                    string dataProtectionJobId = api.GetValuefromResponseBody("JobId", respbody, "JobType", _jobtype_code["dataProtection"]);
                    dataProtectionJob = new DataProtectionJob(t, engine_token, dataProtectionJobId);
                }
                if (jobs_that_staticsync.Contains(type))
                {
                    string staticsync_id = api.GetValuefromResponseBody("JobId", respbody, "JobType", _jobtype_code["staticSync"]);
                    static_syncjob = new OptimizerJob(t, engine_token, staticsync_id);
                }
                if (jobs_that_publish.Contains(type) && t.OceMode)
                {
                    string publish_other_id = api.GetValuefromResponseBody("JobId", respbody, "JobType", _jobtype_code["publishOther"]);
                    publish_other_job = new OCESyncJob(t, engine_token, publish_other_id);
                    string publish_alignments_id = api.GetValuefromResponseBody("JobId", respbody, "JobType", _jobtype_code["publishAlignments"]);
                    publish_alignments_job = new OCESyncJob(t, engine_token, publish_alignments_id);
                }
                return resp.Item1.StatusCode;
            }
            catch
            {
                return resp.Item1.StatusCode;
                throw;
            }
        }
        public bool ScenarioSchemaExists(string schema_suffix)
        {
            string schema = "SC_" + scenario_id.ToUpper() + "_" + schema_suffix;
            if (check_snowflake)
            {
                string sql = "select \"SCHEMA_NAME\" from \"INFORMATION_SCHEMA\".\"SCHEMATA\" where \"SCHEMA_NAME\" = '" + schema + "';";
                return schema == snowq.ReturnSingleColumnValueFromSnowFlake(sql, "SCHEMA_NAME");
            }
            else
            {
                string response = odataq.GetOdata("/OMTerritory", schema, scenario_id);
                //"Error": "SQL compilation error:\nSchema 'TSMOKETESTJAN10D_PROD_AF373D93F26A468DBCD925DFDE40F83B_DB.SC_A1P6D000000PF9AQA_OUTPUT' does not exist or not authorized."
                return !response.Contains(_status_code["schema_not_found"].Replace("{schema}",schema));
            }
        }

        public string ScenarioStatus
        { get
            {
                string status = GetScenarioInfo("ScenarioStatus");
                if (status == null || status == "" || status == "not found") status = _status_code["pending"];
                return status;
            }
        }
        public string GetScenarioInfo(string column)
        {
            if (check_snowflake)
            {
                string sql = "select \""+column+"\" from \"STATIC\".\"OMScenario\" where \"Id\"='" + scenario_id + "';";
                return snowq.ReturnSingleColumnValueFromSnowFlake(sql, column);
            }
            else
            {
                string oreq = "/OMScenario?$select = "+column+" &$filter = Id eq '" + scenario_id + "'";
                return odataq.GetSingleValue(column, oreq, "STATIC", scenario_id);
            }
        }
        public List<string> GetScenarioErrors(bool committing,int max_errors = 5)
        {
            if (check_snowflake)
            {
                string schema = committing ? "OUTPUT" : scenario_output_schema;
                List<string> errors = new List<string> { "TableName|PKey|ErrorCode|ErrorMessage|InstructionId" };
                string sql = "select \"TableName\"||'|'||\"PKey\"||'|'||\"ErrorCode\"||'|'||\"ErrorMessage\"||'|'||\"InstructionId\" \"ErrorDetails\""
                    + " from \""+schema+ "\".\"OMScenarioError\" where \"OMScenarioId\"='" + scenario_id + "' limit " + max_errors;
                errors.AddRange(snowq.ReturnWholeStringColumnValueFromSnowFlake(sql, "ErrorDetails"));
                return errors;
            }
            else
                return new List<string> { "Check OMScenarioError for details" };
        }
        public string GetInternalScenarioId(string scenarioName, int max_wait= default_max_dur, int poll_freq = poll_interval)
        {
            string query = "select \"Id\" from \"STATIC\".\"OMScenario\" where \"Name\" ilike '%" + scenarioName + "%'";
            scenario_id = snowq.ReturnSingleColumnValueFromSnowFlake(query, "Id");

            int elapsed = 0;
            while (scenario_id.Contains("not found") && elapsed < max_wait)
            {
                System.Threading.Thread.Sleep(poll_freq * 1000);
                elapsed += poll_freq;
                scenario_id = snowq.ReturnSingleColumnValueFromSnowFlake(query, "Id");
            }

            return scenario_id;
        }

        public string GetScenarioSyncFinishTime(string scenarioId)
        {
            string query = "select \"SyncEndTime\" from \"STATIC\".\"OMScenario\" where \"Id\" = '" + scenarioId + "'";
            return snowq.ReturnSingleDateTimeColumnValueFromSnowFlake(query, "SyncEndTime").ToString("yyyy-MM-dd HH:mm:ss zzz");
        }

        public string WaitForScenario(string exp_status, int max_dur_sec = default_max_dur, int poll_freq = poll_interval, bool starting = false)
        {
            int elapsed_time = 0;
            List<string> exp_statuses = exp_status.Split(",").ToList();
            bool ignore_error_state = false;
            foreach (string status in exp_statuses) ignore_error_state = ignore_error_state || ignore_error_codes.Contains(status);
            string scenario_status = ScenarioStatus;

            bool start_in_error_state = starting || ignore_error_state; // if already in an error state at start then keep waiting (don't error out yet)
            bool errored_out = !start_in_error_state && error_codes.Contains(scenario_status);

            //wait till status or time-out
            while (!exp_statuses.Contains(scenario_status) && !errored_out && elapsed_time < max_dur_sec)
            {
                start_in_error_state = start_in_error_state && error_codes.Contains(scenario_status); // still in an error state?
                Thread.Sleep(poll_freq * 1000);
                elapsed_time += poll_freq;
                scenario_status = ScenarioStatus;
                errored_out = !start_in_error_state && error_codes.Contains(scenario_status);
            }
            if (error_codes.Contains(scenario_status) && !exp_statuses.Contains(scenario_status)) scenario_status += " | " + GetJobError();
            return scenario_status;
        }
    }

}