using MainFramework;
using MainFramework.GlobalUtils;
using OMREngineTest.Utils;
using Optimizer.Utils;
using System;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    public class BaseEngineSteps
    {
        protected ScenarioContext _scenarioContext;
        protected ITestOutputHelper _output;

        protected EngineTest _engine;
        protected DataSyncTest _datasync;
        protected SchemaSyncTest _schemasync;

        protected TenantConfig _t;
        protected OdataApi _odata;
        protected QuerySnowflake _snowq;

        public string testDataRootDefault = "TestData";
        public string optimizerSettingsDefault = "default";

        public BaseEngineSteps(ScenarioContext scenarioContext, ITestOutputHelper output)
        {
            _output = output;
            _scenarioContext = scenarioContext;
            if (!_scenarioContext.ContainsKey("engine"))
            {
                _engine = new EngineTest(_output);
                _datasync = new DataSyncTest(_output);
                _schemasync = new SchemaSyncTest(_output);
            }
            else
            {
                _engine = _scenarioContext.Get<EngineTest>("engine"); //Set from TenantCreateSteps (Given the tenant configuration defined by (.*)
                _datasync = _scenarioContext.Get<DataSyncTest>("datasync"); //Set from TenantCreateSteps (Given the tenant configuration defined by (.*)
                _schemasync = _scenarioContext.Get<SchemaSyncTest>("schemasync");//Set from TenantCreateSteps (Given the tenant configuration defined by (.*)
            }
            // Add defaults to context if not already set
            if (!_scenarioContext.ContainsKey("testDataRoot")) 
                _scenarioContext["testDataRoot"] = testDataRootDefault;
            if (!_scenarioContext.ContainsKey("optimizerSettings"))
                _scenarioContext["optimizerSettings"] = optimizerSettingsDefault;
        }
        public string GetTestIdFromContext(bool must_exist = false)
        {
            if (must_exist)
                try { 
                    return _scenarioContext.Get<string>("TestUniqueIntegrationId"); }
                catch (Exception e) {
                    Console.WriteLine("Test Id not set. Try using \"Given a unique id for the test\"");
                    throw e;
                }
            else
                if (!_scenarioContext.ContainsKey("TestUniqueIntegrationId"))
                {
                    CodeHelper chelp = new CodeHelper();
                    _scenarioContext["TestUniqueIntegrationId"] = chelp.GenerateTestId(9);
                }
                return _scenarioContext.Get<string>("TestUniqueIntegrationId");
        }

        public string TestDataRoot
        {
            get
            {
                if (_scenarioContext.ContainsKey("testDataRoot"))
                    return _scenarioContext.Get<string>("testDataRoot");
                else
                    return testDataRootDefault;
            }
        }
        public string OptimizerSettings
        {
            get
            {
                if (_scenarioContext.ContainsKey("optimizerSettings"))
                    return _scenarioContext.Get<string>("optimizerSettings");
                else
                    return optimizerSettingsDefault;
            }
        }
        public string TestId
        {
            get
            {
                return GetTestIdFromContext();
            }
        }

        public void LogRequest(string api_info)
        {
            _engine.LogMessage("Logging API request:\n" + api_info);
        }
        public void JobStatusCheck(bool ok, string fail_message, string orig_api_req = null)
        {
            if (!ok && orig_api_req != null)
                LogRequest(orig_api_req);
            Assert.True(ok, fail_message);
        }
        public void Pause(int seconds = 5)
        {
            System.Threading.Thread.Sleep(seconds * 1000);
        }
    }
}
