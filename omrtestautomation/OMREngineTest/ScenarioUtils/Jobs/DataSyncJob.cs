using MainFramework;
using MainFramework.GlobalUtils;
using Newtonsoft.Json;
using OMREngineTest;
using OMREngineTest.Utils;
using Optimizer.Constructors;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OMREngineTest.ScenarioUtils.Jobs
{
    public class DataSyncJob : OptimizerJob
    {
        public DataSyncJob(TenantConfig t, eToken token, string id = null)
            : base(t, token, id) 
        {
            all_statuses = String.Join(",", new List<string> { _status_code["job_pending"], _status_code["job_processing"], _status_code["job_error"], _status_code["job_success"] });
        }

        public HttpStatusCode SubmitDataSyncJob(DataSyncApiRequestBody parameters)
        {
            HttpStatusCode _statuscode = SubmitJob("datasync", parameters);
            api_info = api._request + jobid;
            if (api._response != null) api_info += api._response;
            if (_statuscode == HttpStatusCode.Unauthorized) api_info += api._auth;
            return _statuscode;
        }
        
    }
}