using MainFramework.GlobalUtils;
using MainFramework;
using OMREngineTest.ScenarioUtils.Jobs;
using Xunit;
using System.Net;

namespace Optimizer.ScenarioUtils.Jobs
{
    public class PickListJob : OptimizerJob
    {
        public PickListJob(TenantConfig tenant_info, eToken token)
           : base(tenant_info, token, null)
        {
            api.done_statuses.Add(_status_code["job_success"]);
            api.done_statuses.Add(_status_code["job_error"]);
        }

        public void VerifyPickListJobIsSuccessful()
        {
            Assert.True("OK" == SubmitPickListJob().ToString());
            string jobStatus = api.WaitForStatus(GetPickListJobStatus);
            Assert.True(jobStatus == _status_code["job_success"]);
        }

        protected HttpStatusCode SubmitPickListJob()
        {
            try
            {
                jobid = api.Request_RestSharpValue("jobId", "picklistAccount", "POST");
                return HttpStatusCode.OK;
            }
            catch
            {
                return api.RequestStatus("picklistAccount", "POST").StatusCode;
                throw;
            }
        }

        protected string GetPickListJobStatus()
        {
            return snowq.GetJobStatus(jobid);
        }
    }
}
