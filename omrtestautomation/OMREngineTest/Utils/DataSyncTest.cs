using MainFramework;
using MainFramework.GlobalUtils;
using OMREngineTest.ScenarioUtils.Jobs;
using Optimizer.Constructors;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Optimizer.Utils
{
    public class DataSyncTest
    {
        public static globalCodes _status_code;

        public readonly ITestOutputHelper _output;
        public TenantConfig t; public s3TenantFixture tenant; public bool valid_tenant_config = true;
        public eToken engine_token;

        public DataSyncJob dataSyncJob;

        public DataSyncTest(ITestOutputHelper output)
        {
            _output = output;
            _status_code = new globalCodes("StatusCodes.json");
            //SetTenantConfig(tconfig); //Remove this - this is set in most places
        }


        string FailMessage(string act_status, string testtype = "", int wait_min = 0)
        {
            return "DataSyc " + testtype + " status = " + act_status + (wait_min == 0 ? "" : ", max wait time = " + wait_min + " min");
        }

        public Tuple<bool,string> DataSync(DataSyncApiRequestBody body, string exp_status = "job_success", int wait_min = 0)
        {
            dataSyncJob = new DataSyncJob(t, engine_token);
            
            Assert.Equal(HttpStatusCode.OK, dataSyncJob.SubmitDataSyncJob(body));

            bool has_completed;
            string fail_msg;
            if (wait_min == 0)
            {
                has_completed = dataSyncJob.HasCompleted();
                fail_msg = FailMessage(dataSyncJob.latest_status, "HasCompleted");
            }
            else
            {
                has_completed = dataSyncJob.HasCompleted(wait_min * 60);
                fail_msg = FailMessage(dataSyncJob.latest_status, "HasCompleted", wait_min);
            }

            return new Tuple<bool,string>(has_completed,fail_msg);
        }
    }
}
