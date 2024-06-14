using System;
using System.Collections.Generic;
using System.Text;
using MainFramework.GlobalUtils;
using MainFramework;
using OMREngineTest.ScenarioUtils.Jobs;
using Xunit.Abstractions;

namespace Optimizer.Utils
{
    public class SchemaSyncTest
    {

        public static globalCodes _status_code;

        public readonly ITestOutputHelper _output;
        public TenantConfig t; 
        public s3TenantFixture tenant; 
        public bool valid_tenant_config = true;
        public eToken engine_token;

        public DataSyncJob dataSyncJob;

        public SchemaSyncTest(ITestOutputHelper output)
        {
            _output = output;
            _status_code = new globalCodes("StatusCodes.json");
        }
    }
}
