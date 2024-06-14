using MainFramework.GlobalUtils;
using MainFramework;
using System.Net;

namespace OMREngineTest.ScenarioUtils.Jobs
{
    public struct DataProtectionJobParams
    {
        public string GlobalAccountDataProtectionAction { get; set; }
        public string GlobalUserDataProtectionAction { get; set; }
    }
    public class DataProtectionJob : OptimizerJob
    {
        public DataProtectionJob(TenantConfig t, eToken token, string id)
            : base(t, token, id) { }

        public HttpStatusCode SubmitDataProtectionJob(string globalAccountDataProtectionAction = null, string globalUserDataProtectionAction = null)
        {
            //Submit data protection job manually via API
            DataProtectionJobParams requestBody = new DataProtectionJobParams();
            requestBody.GlobalAccountDataProtectionAction = globalAccountDataProtectionAction;
            requestBody.GlobalUserDataProtectionAction = globalUserDataProtectionAction; 
            return SubmitJob("dataprotection", requestBody);
        }

    }

    
}
