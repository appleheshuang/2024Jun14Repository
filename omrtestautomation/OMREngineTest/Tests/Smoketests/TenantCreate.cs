using System;
using System.Net;
using OMREngineTest;
using Xunit;
using Xunit.Abstractions;
using MainFramework;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using System.Linq;
using System.Collections.Generic;
using OMREngineTest.Utils;
using Region = OMREngineTest.Region;
using OMREngineTest.ScenarioUtils;

namespace SmokeTests
{

    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    [Trait("TestRailProjectId", "95")] // OMR
    [Trait("TestRailSuiteId", "3064")] // Engine tests
    [Trait("TestRailSectionId", "501052")] // SmokeTests
    [Trait("TestType", "Auto-Core")]
    [Trait("custom_testtype", "0")]
    [Trait("custom_tstype", "1")]
    [Trait("custom_testexecution", "2")]
    public class TenantSmoketest : EngineTest
    {
        // Tenant
        private TenantJob _tenantjob;
        private bool valid_master_credentials = true;
        private const string tenantName = "Smoketest tenant";

        public TenantSmoketest(ITestOutputHelper output) : base(output)
        {
            SetTenantConfig(tconfig: "tenantcreate-org.json");
            try { _tenantjob = new TenantJob(t, token_only: !valid_tenant_config ); }
            catch (MissingCredentialsException mce)
            {
                WarningMessage("MissingCredentials exception (" + t.targetEnv + ") - " + mce.Message);
                valid_master_credentials = false;
            }
            string check_org = ValidateOrg("ok");
            if (check_org != "ok") WarningMessage(check_org + " - Tenant create org may have expired");
            initdatafile = "Smoketest/LoadData/InitialBulkload.json";
        }

        [Fact, TestPriority(02)]
        [Trait("References", "OMR-5908,OMR-5482")]
        //OMR-5908 - List API added for tenant
        //OMR-5482 - Reactivate a tenant
        public void TenantListEnable()
        {
            if (valid_master_credentials)
            {
                // Does not list inactive tenants
                Tuple<HttpStatusCode, List<tenantListElement>> tenant_response = _tenantjob.listTenants();
                Assert.Equal(HttpStatusCode.OK, tenant_response.Item1);
                if (tenant_response.Item2.Count == 0) WarningMessage("tenantList returned no tenants");
                else
                    //inactive tenants should not be listed
                    foreach (tenantListElement this_tenant in tenant_response.Item2)
                    {
                        Assert.NotEqual(t.TenantId, this_tenant.tenantId);
                        Assert.NotEqual(TenantStatus.INAC, this_tenant.status);
                    }
                // Enable tenant
                if (valid_tenant_config && t.tenant_exists)
                {
                    string exp_message = _status_code["tenant_enable_success"].Replace("{tenantId}", t.TenantId);
                    // tenant enable request returns successful status
                    string act_message = _tenantjob.tenantEnable(t.TenantId);
                    Assert.Equal(exp_message, act_message);
                    // attempt to get a token for the enabled tenant
                    Assert.Equal(HttpStatusCode.OK, odataq.RequestStatus("token", "GET").StatusCode);

                    // Lists enabled tenants
                    tenant_response = _tenantjob.listTenants();
                    Assert.Equal(HttpStatusCode.OK, tenant_response.Item1);
                    // Check the elements keys in the list
                    bool enabled_tenant_listed = false;
                    foreach (tenantListElement this_tenant in tenant_response.Item2)
                    {
                        Assert.Equal(TenantStatus.ACTV, this_tenant.status);
                        if (this_tenant.tenantId == t.TenantId)
                        {
                            enabled_tenant_listed = true;
                            Assert.Equal(TenantComputeType.Shared, this_tenant.computeType);
                            Assert.Equal(TenantType.PROD, this_tenant.tenantType);
                            Assert.Equal(40, this_tenant.noOfTerritories);
                            Assert.Equal(t.lexiApiKey, this_tenant.apiKeys.lexiApiKey);
                            Assert.Equal(t.sfApiKey, this_tenant.apiKeys.salesForceApiKey);
                            Assert.Equal(tenantName, this_tenant.tenantName);
                        }
                    }
                    Assert.True(enabled_tenant_listed, "List tenants - enabled tenant not listed. TenantId=" + t.TenantId);
                }
            }
        }

        [Fact, TestPriority(01)]
        [Trait("References", "OMR-2405")]
        //OMR-2405 - PurgeInactiveTenant API (GET only)
        public void TenantGetDisabled()
        {
            if (valid_master_credentials)
            {
                // List tenants to be purged
                Tuple<HttpStatusCode, List<tenantToBePurgedElement>> tenant_response = _tenantjob.listTenantsToPurge();
                Assert.Equal(HttpStatusCode.OK, tenant_response.Item1);
                // Check the elements keys in the list
                List<tenantToBePurgedElement> tenant_list = tenant_response.Item2;
                if (tenant_list.Count == 0) WarningMessage("tenantsToBePurged returned no tenants");
                else
                foreach (tenantToBePurgedElement x in tenant_list)
                {
                    Assert.IsType<string>(x.tenantId);
                    Assert.IsType<string>(x.tenantName);
                    Assert.IsType<DateTime>(x.disabledOn);
                    Assert.IsType<TenantType>(x.tenantType);
                }
            }
        }

    }
}
