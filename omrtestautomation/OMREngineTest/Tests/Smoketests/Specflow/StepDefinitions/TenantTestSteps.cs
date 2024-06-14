using MainFramework;
using MainFramework.GlobalUtils;
using System.Collections.Generic;
using OMREngineTest.Utils;
using System;
using System.Linq;
using System.Net;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.UnitTestProvider;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json.Linq;
using Optimizer.Utils;
using System.IO;
using Amazon.DynamoDBv2.Model;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    public class TenantTestSteps : BaseEngineSteps
    {

        private TenantJob _tenantjob;

        private bool valid_master_credentials = true;
        private const string tenantName = "Smoketest tenant";
        private readonly IUnitTestRuntimeProvider _unitTestRuntimeProvider;

        OptimizerApi checkver;
        private string tenant_version;
        const string dynamo_tenant_tablename = "OMRTenantConfigurations";

        public TenantTestSteps(ScenarioContext scenarioContext, IUnitTestRuntimeProvider unitTestRuntimeProvider, ITestOutputHelper output) : base(scenarioContext, output)
        {
            _unitTestRuntimeProvider = unitTestRuntimeProvider;
        }

        [Given(@"the tenant configuration defined by '(.*)'")]
        public void GivenTheTenantConfigurationDefinedBy(string tconfig)
        {
            //Skip ocesync org for Demo env
            string target_env = Environment.GetEnvironmentVariable("TARGET_ENV");
            if (tconfig.ToLower().Contains("oce") && target_env.ToLower().Contains("demo"))
            {
                _unitTestRuntimeProvider.TestIgnore("Skipping OCE test in demo env. TenantConfig=" + tconfig + ", Env=" + target_env);
            }
            else
            {
                _scenarioContext.Set<string>(tconfig, "tconfig");
                if (_engine.SetTenantConfig(tconfig)) 
                { 
                    _scenarioContext.Set<EngineTest>(_engine, "engine");
                    _datasync.t = _engine.t;
                    _datasync.engine_token = _engine.engine_token;
                    _scenarioContext.Set<DataSyncTest>(_datasync, "datasync");
                    _schemasync.t = _engine.t;
                    _schemasync.engine_token = _engine.engine_token;
                    _scenarioContext.Set<SchemaSyncTest>(_schemasync, "schemasync");
                    _snowq = _engine.snowq;
                    _odata = _engine.odataq;
                    _t = _engine.t;
                    _output.WriteLine(tconfig + " -> " + _t.targetEnv + " (" + _t.endpoint + "), SF Org: " + _t.baseURL + "");
                }
                else
                    if (tconfig.ToLower().Contains("oce"))
                        _unitTestRuntimeProvider.TestIgnore("Skipping OCE test for target env. Tenant config not found on s3. TenantConfig=" + tconfig + ", Env=" + target_env);
            }
        }
        [Given(@"I check queue health for the env")]
        public void GivenICheckQueueHealthForTheEnv()
        {
            int queue_count = _engine.odataq.QueuedJobCount();
            Assert.True(queue_count < EngineTest.max_queue_load_at_start, "Aborting test... System overload on " + _engine.t.targetEnv + "\n\t Queue count=" + queue_count+ " is higher than threshold queue load at test start ("+ EngineTest.max_queue_load_at_start+")");
        }
        [Then(@"I check engine is running")]
        public void ThenICheckEngineStatus()
        {
            bool engine_is_running = _engine.odataq.EngineIsRunning();

            //Allow engine some time to start up post-deployment
            int retryCount = -1;
            int maxRetryCount = 10;
            int interval = 30;
            for (int i = 1; i < maxRetryCount + 1 && !engine_is_running; i++)
            {
                _engine.LogMessage("/EngineStatus retry attempt number: " + i + " ...");
                engine_is_running = _engine.odataq.EngineIsRunning();
                _engine.Pause(interval);
                retryCount = i;
            }
            Assert.True(engine_is_running, "Engine was not running with interval: " + interval + " seconds and " + retryCount + " retries.");
        }
        [Then(@"I check connectivity status")]
        public void ThenICheckConnectivityStatus()
        {
            connectivity_node connectivity_status = _engine.odataq.GetConnectivityStatus();
            Assert.Equal("OK", connectivity_status.OMR);
            Assert.Equal(_engine.t.OceMode ? "OK" : "NA", connectivity_status.OCE);
        }
        [Then(@"I check '(.*)' status is '(.*)'")]
        public void ThenICheckSystemStatusNode(string node, string expresult)
        {
            string datasyncstatus = _engine.odataq.SystemStatusNode(node);
            Assert.Equal(expresult, datasyncstatus);
        }

        [Given(@"I want '(.*)' with '(.*)' schema")]
        public void GivenIWantPackageWithVersionSchema(string nodeName, string versionNumber)
        {
            string endpoint;
            switch (nodeName.ToLower())
            {
                case "oceo":
                    endpoint = "engineVersion";
                    break;
                case "oced":
                    endpoint = "digitalVersion";
                    break;
                case "oceseg":
                    endpoint = "segmentationVersion";
                    break;
                default:
                    endpoint = "engine";
                    break;
            }

            if (versionNumber == "latest")
            {
                checkver = new OptimizerApi(_engine.t);

                versionNumber = checkver.Request_RestSharp_NoAuth(endpoint, "GET").Item2.ToString();
                JObject response_as_json = JObject.Parse(versionNumber);
                if (response_as_json.ContainsKey("apiVersion"))
                    tenant_version = response_as_json["apiVersion"].ToString().Replace('-','.');
                else
                    throw new Exception("Engine response does not contain apiVersion: " + versionNumber);
            } else
            {
                tenant_version = versionNumber;
            }

            _tenantjob.AddPackageNode(nodeName, tenant_version);
        }

        [Given(@"I want OptimizerEnabled = (.*)")]
        public void GivenIWantOptimizerEnabled(bool optimizerEnabled)
        {
            _tenantjob.SetOptimizerEnabled(optimizerEnabled);
        }

        [Given(@"the package version for the tenant")]
        public void GivenThePackageVersionForTheTenant()
        {
            TenantConfig t = _engine.t;
            eToken engine_token = _engine.engine_token;
            if (engine_token.versionendpoint == null || engine_token.token == null)
            {
                _engine.SetTenantConfig(_scenarioContext.Get<string>("tconfig"));
                engine_token = _engine.GetEngineToken();
            }
            tenant_version = engine_token.versionendpoint;

            checkver = new OptimizerApi(t, engine_token);
            checkver.SetVersion();

            string engineVersion = checkver.Request_RestSharp_NoAuth("engineVersion", "GET").Item2.ToString();
            if (checkver.engine_version == "latest")
            {
                JObject response_as_json = JObject.Parse(engineVersion);
                if (response_as_json.ContainsKey("apiVersion"))
                    tenant_version = response_as_json["apiVersion"].ToString();
                else
                    throw new Exception("Engine repsonse does not contain apiVersion: " + engineVersion);
            }
        }
        [Then(@"the '(.*)' matches the package version")]
        public void ThenTheMatchesThePackageVersion(string service_endpoint)
        {
            string serviceVersion = checkver.Request_RestSharp_NoAuth(service_endpoint, "GET").Item2.ToString();
            Assert.Contains(tenant_version, serviceVersion);
        }
        [Then(@"the snowflake version matches the package version")]
        public void ThenTheSnowflakeVersionMatchesThePackageVersion()
        {
            //check snowflake version
            if (_engine.t.snowchecks_enabled)
            {
                QuerySnowflake snowq = new QuerySnowflake(_engine.t.snowConnection);
                string[] exp_major_minor = tenant_version.Split('-');
                exp_major_minor[1] = "00".Remove(2 - exp_major_minor[1].Length) + exp_major_minor[1];
                string exp_snowversion = exp_major_minor[0] + '.' + exp_major_minor[1];
                string version_query = "select \"Version\" from \"OUTPUT\".\"SchemaVersion\"";
                string actual_version = snowq.ReturnSingleColumnValueFromSnowFlake(version_query, "Version");
                if (!actual_version.StartsWith(exp_snowversion)) { 
                    if (checkver.engine_version != "latest") 
                        _engine.FatalErrorHandler("SchemaVersion " + actual_version + " does not match expected: " + exp_snowversion);
                    else 
                        _engine.WarningMessage("Latest SchemaVersion " + actual_version + " does not match version expected: " + exp_snowversion);
                }
            }
        }


        [Given(@"I validate the Salesforce org")]
        public void GivenIValidateTheSalesforceOrg()
        {

            try { _tenantjob = new TenantJob(_engine.t, token_only: !_engine.valid_tenant_config); }
            catch (MissingCredentialsException mce)
            {
                _output.WarningMessage("MissingCredentials exception (" + _engine.t.targetEnv + ") - " + mce.Message);
                valid_master_credentials = false;
            }
            string check_org = _engine.ValidateOrg("ok");
            if (check_org != "ok") _output.WarningMessage(check_org + " - Tenant create org may have expired");
            _engine.initdatafile = "Smoketest/LoadData/InitialBulkload.json";

            _output.LogMessage("Tenant create/disable for " + _engine.t.targetEnv + " - " + _engine.t.orgURL + " (" + _engine.t.endpoint + _engine.t.targetVersion + ") ...");
            if (_engine.valid_tenant_config && valid_master_credentials)
            {
                //Console.WriteLine("\nTenant create...");
                Assert.Equal("Salesforce org validated", _engine.ValidateOrg("Salesforce org validated"));
            }
            else if (!valid_master_credentials)
            {
                Console.WriteLine("\t Tenant create credentials not found - skipping tenant create");
                _unitTestRuntimeProvider.TestIgnore("\t Tenant create credentials not found - skipping tenant create");
            }
            else
            {
                // If we don't have a valid sf org, just check master tenant creator can get a token
                Console.WriteLine("\t tenant-creator check only");
                try
                {
                    _tenantjob = new TenantJob(_engine.t, true);
                }
                catch
                {
                    _output.FatalErrorHandler("Failed to get a token for _" + _engine.t.targetEnv + "_ tenantcreator");
                }
                Assert.NotNull(_tenantjob.engine_token.token);
            }

        }

        [When(@"I create a tenant")]
        public void WhenICreateATenant()
        {
            Tuple<response_status_message, TenantConfig> new_tenant_response = _tenantjob.tenantCreate(tenantName);
            _output.LogMessage("Newly created TenantId: " + new_tenant_response.Item2.TenantId + " with SalesForceAPIKey: " + new_tenant_response.Item2.sfApiKey);

            Assert.Equal(HttpStatusCode.OK, new_tenant_response.Item1.statuscode);
            Assert.Contains(EngineTest._status_code["tenant_create_async"], new_tenant_response.Item1.responsemessage);        

            // update tenant info t.x
            Type ty = typeof(TenantConfig);
            var properties = ty.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);
            foreach (var prop in properties)
            {
                var value = prop.GetValue(new_tenant_response.Item2, null);
                if (value != null)
                    prop.SetValue(_engine.t, value, null);
            }
        }

        [Then(@"the tenant creation is successful")]
        public void ThenTheTenantCreationIsSuccessful()
        {
            // check tenant is created successfully 
            string exp_status = EngineTest._status_code["tenant_create_success"];
            string create_status = _tenantjob.WaitForStatus(_tenantjob.JobStatus);
            Assert.True(exp_status == create_status, "Tenant create failed with error -> " + _tenantjob.JobError());
            Assert.Equal("Tenant is valid", _engine.ValidateTenant("Tenant is valid"));
            _engine.odataq = new OdataApi(_engine.t, _engine.engine_token); //Set Odata once tenant is valid;
        }
        [Then(@"I get the snowflake credentials")]
        public void ThenIGetTheSnowflakeCredentials()
        {
            try
            {
                DynamoHelper dh = new DynamoHelper("s3TestBucket.json", _engine.t.targetEnv);
                _engine.t.snowConnection = dh.CollectSnowFlakeDetails(_engine.t.TenantId);
            }
            catch (Exception e)
            {
                throw new Exception("Error retriveing Snowflake node in dynamo. TenantId=" + _engine.t.TenantId, e);
            }
            _snowq = new QuerySnowflake(_engine.t.snowConnection);
        }

        [Then(@"the tenant creation is successful and I can get a token and tenant SalesForceXApiKey is generic")]
        public void ThenTheTenantCreationIsSuccessfulAndICanGetATokenAndTenantSalesForceXApiKeyIsGeneric()
        {
            ThenTheTenantCreationIsSuccessful();
            //checking that tenant is created with a generic x-api-key OMR-12601
            Assert.Equal(_engine.t.generic_sfApiKey, _engine.t.sfApiKey);
        }


        [Then(@"the tenant '(.*)' status is '(.*)'")]
        public void ThenTheTenantStatusIs(string status_key, string exp_value)
        {
            string act_value = _engine.odataq.GetSingleValue(status_key, "/SystemStatus", "STATIC");
            Assert.Equal(exp_value, act_value);
        }

        [Then(@"check scenarioless results per '(.*)'")]
        public void ThenCheckScenarioLessResultsPer(string file)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            Assert.Equal("OK",_engine.RunDataTests(_scenarioContext, file));     
        }

        [When(@"I disable the tenant that was just created")]
        public void WhenIDisableTheTenantThatWasJustCreated()
        {
            // Disable the tenant that was just created
            if (_engine.valid_tenant_config && _engine.t.tenant_exists)
            {
                //Console.WriteLine("\nTenant disable...");
                //bool tenant_enabled_at_start = true;
                string exp_message = EngineTest._status_code["tenant_disable_success"].Replace("{tenantId}", _engine.t.TenantId);
                try
                {
                    _engine.odataq = new OdataApi(_engine.t, _engine.engine_token); //initialize odata for the tenant
                }
                catch
                {
                    //tenant_enabled_at_start = false;
                    exp_message = EngineTest._status_code["tenant_already_disabled"].Replace("{tenantId}", _engine.t.TenantId);
                }

                // tenant disable request returns successful status
                string act_message = _tenantjob.tenantDisable(_engine.t.TenantId);
                Assert.Equal(exp_message, act_message);
            }
        }

        [When(@"I pause for the change to take effect")]
        public void WhenIPauseForTheChangeToTakeEffect()
        {
            Pause(10);
        }

        public enum exp_gettoken_status {fails, successful };
        [Then(@"GetToken (.*) for the tenant")]
        public void ThenGetToken(exp_gettoken_status exp_status) {
            // attempt to get a token for the tenant
            HttpStatusCode expected_status = exp_status == exp_gettoken_status.successful ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
            Assert.Equal(expected_status, _engine.odataq.RequestStatus("token", "GET").StatusCode);
        }
        public enum exp_replication_status { on, off };
        [Then(@"tenant replication is (.*)")]
        public void ThenTenantReplicationIs(exp_replication_status exp_status) {
            string expected_status = exp_status == exp_replication_status.on ? "OK" : "OFF";
            connectivity_node connectivity_status = _engine.odataq.GetConnectivityStatus();
            Assert.Equal(expected_status, connectivity_status.OMR);
            Assert.Equal(_engine.t.OceMode ? expected_status : "NA", connectivity_status.OCE); 
        }

        /// <summary>
        /// Whitelist tables in the snowflake db so they can be queried by the browser
        /// </summary>
        /// <param name="whitelist">csv file with tables/view to whitelist</param>
        [Given(@"I whitelist snowflake tables")]
        public void GivenIWhitelistSnowflakeTables(Table whitelist)
        {
            const string insertIntoWhiteList = "insert into PUBLIC.\"BrowserAllowedObjects\" (\"schema\",\"name\",\"type\") VALUES ('{schema}','{table}',{type})";
            const string updateWhiteList = "update PUBLIC.\"BrowserAllowedObjects\" set \"schema\" = '{schema}',\"type\" = {type} where \"Id\" = '{Id}'";
            string use_query;
            foreach (var row in whitelist.Rows)
            {
                string type = "0"; //table by default
                if (whitelist.ContainsColumn("Type"))
                    try
                    {
                        int.Parse(row["Type"]); 
                        type = row["Type"];
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Whitelist type should be an integer: 0=table, 1=view. Actual value=" + row["Type"].ToString(), e);
                    }
                try
                {
                    if (_t.snowConnection is null) ThenIGetTheSnowflakeCredentials();
                    string id = _snowq.LookupValue(tablename: "BrowserAllowedObjects", lookup_clm: "Id", known_clm: "name", known_value: row["TableName"], schema: "PUBLIC");
                    use_query = _snowq.NotFound(id) ? insertIntoWhiteList : updateWhiteList;
                    _snowq.RunQuery(use_query.Replace("{schema}", row["Schema"]).Replace("{table}", row["TableName"]).Replace("{type}", type).Replace("{Id}", id));
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to whitelist table or view: \n\t" + whitelist.Header.ToString() + "\n\t" + row.ToString(), e);
                }
            }
        }

        /// <summary>
        /// Create/update the snowflake user so that the browser can query whitelisted tables
        /// </summary>
        [Given(@"I create the snowflake user for browser access")]
        public void GivenICreateTheSnowflakeUserForBrowserAccess()
        {
            TenantBrowserJob createBrowserUser = new TenantBrowserJob(_engine.t);
            string expectedResponse = OptimizerApi._status_code["browser_user_created"]
                .Replace("{tenantId}", _engine.t.TenantId);

            Assert.Contains(expectedResponse, createBrowserUser.CreateBrowserUser());
        }

        [When(@"I use lexi-api-key instead of sf-api-key")]
        public void WhenIUseLexi_Api_KeyInsteadOfSf_Api_Key()
        {
            _t.sfApiKey = _t.lexiApiKey;
        }


        [Given(@"the test data path (.*)")]
        public void GivenTheDatapath(string testsubdir)
        {
            _scenarioContext["testDataRoot"] = Path.Combine(testDataRootDefault, testsubdir);
        }

        [Given(@"the optimizer settings '(.*)'")]
        public void GivenTheOptimizerSettings(string settings)
        {
            _scenarioContext["optimizerSettings"] = settings;
        }


    }

}
