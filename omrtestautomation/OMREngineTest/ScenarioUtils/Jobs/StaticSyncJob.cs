using System.Net;
using MainFramework;
using MainFramework.GlobalUtils;

namespace OMREngineTest.ScenarioUtils.Jobs
{
    public struct staticsync_params
    {
        public string StartFrom { get; set; }
        public bool Resync { get; set; }
        public bool SyncAll { get; set; }
        public string Tables { get; set; }
    }

    public class StaticSyncJob : OptimizerJob
    {
        public StaticSyncJob(TenantConfig t, eToken token, string id)
            : base(t, token, id)
        {
            api.done_statuses.Add("OK");
        }

        public HttpStatusCode StaticSyncTestData(bool resync, string startfrom = null, string tables = null)
        {
            //Run SyncJob
            staticsync_params syncBody = new staticsync_params();
            syncBody.StartFrom = startfrom;
            syncBody.Resync = resync;
            syncBody.Tables = tables;
            syncBody.SyncAll = (tables == null) ? true : false;
            return SubmitJob("staticsync", syncBody);
        }
    }
}