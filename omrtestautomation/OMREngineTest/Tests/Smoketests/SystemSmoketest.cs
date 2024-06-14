using System;
using System.Net;
using System.Linq;
using OMREngineTest;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using OMREngineTest.Utils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.ScenarioUtils.Jobs;

namespace SmokeTests
{

    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    [Trait("TestRailProjectId", "95")] // OMR
    [Trait("TestRailSuiteId", "3064")] // Engine tests
    [Trait("TestRailSectionId", "304984")] // SmokeTests
    [Trait("TestType", "Auto-Regression")]
    [Trait("custom_testtype", "0")]
    [Trait("custom_tstype", "1")]
    [Trait("custom_testexecution", "2")]
    public class SystemSmoketest : EngineTest
    {

        public SystemSmoketest(ITestOutputHelper output) : base(output)
        {
            SetTenantConfig(tconfig: "smoketest-org.json");
        }

        [Fact, TestPriority(01)]
        [Trait("References", "")]
        public void ValidateOrgTenant()
        {
            string[] all_configs = new string[] { "smoketest-org.json", "ocesync-org.json" };
            string target_env = Environment.GetEnvironmentVariable("TARGET_ENV");
            foreach(string tconfig in all_configs)
            {
                //Skip ocesync org for Demo env
                if (!tconfig.ToLower().Contains("oce") || !target_env.ToLower().Contains("demo"))
                {
                    SetTenantConfig(tconfig);
                    string testtype = tconfig.Replace("-org.json", "");
                    OptimizerApi checkver = new OptimizerApi(t, engine_token);
                    checkver.SetVersion();
                    LogMessage("Running " + testtype + " tests for " + t.targetEnv + " (" + t.endpoint + checkver.engine_version + ")\r\n For Org with URL: " + t.baseURL + "");
                    //Sf org is valid
                    Assert.Equal("Salesforce token obtained", ValidateOrg("Salesforce token obtained"));
                    //Tenant credentials
                    Assert.Equal("Tenant token obtained", ValidateTenant("Tenant token obtained"));
                    //Snowflake connection
                    if (t.snowchecks_enabled) Assert.Equal("Snowflake connection obtained", ValidateSnowflake("Snowflake connection obtained"));
                }
            }
            SetTenantConfig(tconfig: "smoketest-org.json");
        }

    }
}
