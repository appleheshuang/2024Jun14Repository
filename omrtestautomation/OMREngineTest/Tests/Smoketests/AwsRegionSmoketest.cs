using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MainFramework;
using MainFramework.GlobalUtils;
using OMREngineTest;
using OMREngineTest.ScenarioUtils;
using OMREngineTest.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SmokeTests
{

    //[TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    [Trait("TestRailProjectId", "95")] // OMR
    [Trait("TestRailSuiteId", "3064")] // Engine tests
    [Trait("TestRailSectionId", "304984")] // SmokeTests
    [Trait("TestType", "Auto-Regression")]
    [Trait("custom_testtype", "0")]
    [Trait("custom_tstype", "1")]
    [Trait("custom_testexecution", "2")]
    public class AwsRegionSmoketest
    {
        // Test Data to load
        const string testconfigdir = "AwsRegionOrgs";
        const string testdatafile = "Smoketest\\Scenario\\OriginalSmoketest.json";
        private string[] regional_orgs;
        public readonly ITestOutputHelper _output;

        public AwsRegionSmoketest(ITestOutputHelper output)
        {
            s3TenantFixture tf = new s3TenantFixture();
            regional_orgs = tf.GetConfigs(testconfigdir);
            _output = output;
        }

    }
}