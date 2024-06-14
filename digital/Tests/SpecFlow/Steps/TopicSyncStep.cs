using MainFramework;
using OMREngineTest.ScenarioUtils.DataUnmarshallers;
using OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions;
using System;
using System.Collections.Generic;
using TechTalk.SpecFlow;
using Xunit.Abstractions;
using System.Reflection;

namespace Digital.Tests.SpecFlow.Steps
{
    [Binding]
    public class TopicStepDefinitions : BaseEngineSteps
    {
        public TopicStepDefinitions(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
        }

        [Given("I patch (.*) Objects '(.*)' in Salesforce with updates from file '(.*)'")]
        public void PatchTopics(sf_obj_type type,string objectType, string filePath)
        {
            string tconfig = _scenarioContext.Get<string>("tconfig");
            string uid = GetTestIdFromContext();
            SalesforceUnmarshaller salesforceUnmarshaller = new SalesforceUnmarshaller(_scenarioContext, tconfig);
            salesforceUnmarshaller.SetTestId(uid);
            IEnumerable<Object> objectList = _scenarioContext.Get<List<Object>>(objectType+"s");
            foreach (Object objs in objectList)
            {
                switch (objectType)
                {
                    case "Topic": salesforceUnmarshaller.PostSFObjectsFromFilePath(type, objectType, filePath, "patch", ((TopicOCED)objs).Id); break;
                    case "TopicProduct": salesforceUnmarshaller.PostSFObjectsFromFilePath(type, objectType, filePath, "patch", ((TopicProduct)objs).Id); break;
                    //case "Territory2": salesforceUnmarshaller.PostSFObjectsFromFilePath(type, objectType, filePath, "patch", ((Territory2)objs).Id); break;
                }
            }
        }
    }
}
