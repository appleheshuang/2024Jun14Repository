using MainFramework.GlobalUtils;
using TechTalk.SpecFlow;

namespace OMREngineTest.ScenarioUtils
{
    public class OptimizerUnmarshaller : JSONUnmarshaller
    {
        protected SalesforceApi sf;
        public string domain;

        public OptimizerUnmarshaller(ScenarioContext scenarioContext, string tenantconfigfile = "smoketest-org.json", string testid = null, string sftoken = null, string datapath = null)
            : base(scenarioContext, tenantconfigfile, testid, datapath)
        {
            if (sftoken == null)
                sf = new SalesforceApi(tenant.Config);
            else
                sf = new SalesforceApi(sftoken, tenant.Config.orgURL, tenant.Config.sfnamespace);

            int domainStart = tenant.Config.orgURL.IndexOf("https://") + "https://".Length;
            int domainEnd = tenant.Config.orgURL.IndexOf(".");
            domain = tenant.Config.orgURL.Substring(domainStart, domainEnd - domainStart);

            SetInitialDataMapValues();
        }

        private void SetInitialDataMapValues()
        {
            string sfUserId = sf.GetSFUserId(tenant.Config.orgUserName);
            dataMap["sfuser_id"] = sfUserId;
            dataMap["domain"] = domain;
            dataMap["prefix"] = tenant.Config.sfnamespace + (tenant.Config.sfnamespace.EndsWith('_') ? "" : "__");
            dataMap["TestUniqueIntegrationId"] = SetTestId();
        }
    }
}
