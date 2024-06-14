using System.Linq;
using System.Net;
using System.Collections.Generic;
using MainFramework.GlobalUtils;
using System.Threading;
using MainFramework;
using System;
using RestSharp;

namespace OMREngineTest.ScenarioUtils.Jobs
{

    public class OptimizerJob
    {
        public eToken engine_token;
        public QuerySnowflake snowq; public OdataApi odataq; public OptimizerApi api;
        public bool check_snowflake;
        public string latest_status;
        public string all_statuses;
        public static globalCodes _status_code = new globalCodes("StatusCodes.json");
        public static globalCodes _jobtype_code = new globalCodes("JobCodes.json");
        public const int poll_interval = 10;
        public const int default_max_dur = 600;
        public TenantConfig t;

        public string jobid { get; set; }
        public string api_info { get; set; }

        const string starttime_label = "StartDateTime";
        const string endtime_label = "EndDateTime";

        List<string> done_statuses = new List<string> { _status_code["job_success"], _status_code["job_error"], _status_code["job_skipped"], _status_code["completed_with_warnings"] };
        List<string> success_statuses = new List<string> { _status_code["job_success"] };
        string completed_status { get { return string.Join(',', done_statuses); } }
        string successful_status { get { return string.Join(',', success_statuses); } }

        public OptimizerJob(TenantConfig tenant_info, eToken token, string id)
        {
            string db = tenant_info.snowConnection;
            t = tenant_info;
            jobid = id;
            engine_token = token;
            check_snowflake = !(db is null || db == "");
            if (check_snowflake) snowq = new QuerySnowflake(db);
            else odataq = new OdataApi(t, token);
            all_statuses = String.Join(",", new List<string> { _status_code["job_pending"], _status_code["job_processing"], _status_code["job_error"], _status_code["job_success"] });
            api = new OptimizerApi(tenant_info, token);

        }
        public HttpStatusCode SubmitJob(string jobtype, object requestBody)
        {
            try
            {
                jobid = api.Request_RestSharpValue("JobId", jobtype, "POST", requestBody);
                //api_info = api._request;
                return HttpStatusCode.OK;
            }
            catch
            {
                return api.RequestStatus(jobtype, "POST", requestBody).StatusCode;
                throw;
            }
        }

        public string JobStatus()
        {
            latest_status = GetJobInfo("Status") ?? "";
            return latest_status;
        }
        public string GetJobError()
        {
            string error_message = GetJobInfo("ErrorMessage");
            if (error_message.Length > 100) error_message.Substring(0, 100); // truncate to 100 chars
            return error_message;
        }
        public int GetJobTiming()
        {
            Tuple<IRestResponse, String> response = api.Request_RestSharp("datasyncStatus", "GET", api_param: jobid);
            try
            {
                DateTime start_time = DateTime.Parse(api.GetValuefromResponseBody(starttime_label, response.Item2));
                DateTime end_time = DateTime.Parse(api.GetValuefromResponseBody(endtime_label, response.Item2));
                return (int)(end_time - start_time).TotalSeconds;
            }
            catch
            {
                Console.WriteLine("GetJobTiming: Error parsing time diff between " + starttime_label + " and " + endtime_label + ". \nResponse info=" + response.Item2);
                return -1;
            }
        }

        public string GetJobInfo(string column)
        {
            string jobstatus = api.Request_RestSharpValue(column, "datasyncStatus", "GET", api_param: jobid);
            return jobstatus;
        }

        public string WaitForStatus(string exp_status, int max_dur_sec = default_max_dur, int poll_freq = poll_interval)
        {
            int elapsed_time = 0;
            List<string> exp_statuses = exp_status.Split(",").ToList();
            string job_status = JobStatus();

            //wait till status or time-out
            while (!exp_statuses.Contains(job_status) && !job_status.ToLower().Contains("error") && elapsed_time < max_dur_sec)
            {
                Thread.Sleep(poll_freq * 1000);
                elapsed_time += poll_freq;
                job_status = JobStatus();
            }
            return job_status;
        }
        public bool IsPending(int waitime=0)
        {
            return WaitForStatus(_status_code["job_pending"], waitime) == _status_code["job_pending"];
        }
        public bool IsProcessing(int waitime = 60)
        {
            return WaitForStatus(_status_code["job_processing"], waitime) == _status_code["job_processing"];
        }
        public bool HasStarted(int waitime = 60)
        {
            string started_statuses = _status_code["job_processing"] + "," + _status_code["job_success"] + "," + _status_code["job_error"];
            return started_statuses.Contains(WaitForStatus(started_statuses, waitime));
        }
        public bool HasCompleted(int waitime = 300)
        {
            return done_statuses.Contains(WaitForStatus(completed_status, waitime));
        }
        public bool IsSuccessful(int waitime = 300)
        {
            return success_statuses.Contains(WaitForStatus(successful_status, waitime));
        }
        public bool HasFailed(int waitime = 300)
        {
            return WaitForStatus(_status_code["job_error"], waitime) == _status_code["job_error"];
        }
        public string StartTime()
        {
            if (HasStarted())
                return GetJobInfo("StartDateTime");
            else
                return null;
        }
        public string EndTime()
        {
            if (HasCompleted())
                return GetJobInfo("EndDateTime");
            else
                return null;
        }
    }

}