using AngleSharp.Dom;
using Google.Protobuf.WellKnownTypes;
using MainFramework;
using MainFramework.GlobalUtils;
using Newtonsoft.Json.Linq;
using OMREngineTest.ScenarioUtils.Jobs;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Xunit;

namespace Digital.DigitalUtils.Jobs
{
    class DigitalDataSyncJob : OptimizerJob
    {
        List<string> jobIds = new List<string>();
        public DigitalDataSyncJob(TenantConfig t, eToken token, string id = null)
    : base(t, token, id)
        {

        }

        public void SubmitScheduleDatasyncCache(string type,string cache)
        {
            JObject parameter = new JObject
            {
             { "SFMCIgnoreCache", cache }
            };

            JObject body = new JObject
            {
                { "Type", type },
                { "TenantId", t.TenantId },
                { "Parameters",parameter}
            };

            Tuple<IRestResponse, string> response = api.Request_RestSharp("submitSchedularJob", "POST", isDigital: true, request_body: body);
            jobIds = api.GetValuesfromResponseBody("JobId", response.Item2);
        }

        public void SubmitScheduleDatasyncCacheExcludedAliases(string type, string cache, string ExcludedAliases)
        {
            JObject parameter = new JObject
            {
             { "SFMCIgnoreCache", cache },
             //{ "SelectedAliases", "OMAccount" },
             { "ExcludedAliases", ExcludedAliases }
            };

            JObject body = new JObject
            {
                { "Type", type },
                { "TenantId", t.TenantId },
                { "Parameters",parameter}
            };

            Tuple<IRestResponse, string> response = api.Request_RestSharp("submitSchedularJob", "POST", isDigital: true, request_body: body);
            jobIds = api.GetValuesfromResponseBody("JobId", response.Item2);
        }

        public void ResetInitialSyncSingleObject(string OMTableName, string DEExternalKey)
        {
            JObject body = new JObject
            {
                { "OMTableName", OMTableName },
                { "DEExternalKey", DEExternalKey }
            };
            Tuple<IRestResponse, string> response = api.Request_RestSharp("DigitalResetInitialSync", "POST", isDigital: true, request_body: body);
            Assert.Contains("successful", response.Item2);
        }

        public bool IsAllSuccessful(int waitime = 1200)
        {
            foreach (string jobId in jobIds)
            {
                this.jobid = jobId;
                string success_or_error = _status_code["job_success"] + ',' + _status_code["job_error"];
                if (WaitForStatus(success_or_error, waitime) == _status_code["job_success"])
                    continue;
                else
                    return false;
            }
            return true;
        }

        public bool IsAllFailed(int waitime = 1200)
        {
            foreach (string jobId in jobIds)
            {
                this.jobid = jobId;
                string success_or_error = _status_code["job_success"] + ',' + _status_code["job_error"];
                if (WaitForStatus(success_or_error, waitime) == _status_code["job_error"])
                    continue;
                else
                    return false;
            }
            return true;
        }

        public bool IsAllComplete(int waitime = 1200)
        {
            foreach (string jobId in jobIds)
            {
                this.jobid = jobId;
                string success_or_error = _status_code["job_success"] + ',' + _status_code["job_error"];
                if (WaitForStatus(success_or_error, waitime) == _status_code["job_error"]|| WaitForStatus(success_or_error, waitime)==_status_code["job_success"])
                    continue;
                else
                    return false;
            }
            return true;
        }
    }



}

