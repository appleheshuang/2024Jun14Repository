using Newtonsoft.Json.Linq;
using OMREngineTest.Constructors.SalesforceObjects.OCEObjects;
using System;
using System.Collections.Generic;
using System.Text;
using TechTalk.SpecFlow;

namespace OMREngineTest.ScenarioUtils.DataUnmarshallers
{
    class OCEJSONUnmarshaller : OptimizerUnmarshaller
    {

        public OCEJSONUnmarshaller(ScenarioContext scenarioContext, string tconfig) : base(scenarioContext, tconfig) { }

        public void PostSetupOCEObjectsFromFilePath(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);

            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                var custAlignRules = jsonObj["CustomerAlignmentRule"]?.ToObject<List<CustomerAlignmentRule>>();
                foreach (CustomerAlignmentRule custAlignRule in custAlignRules)
                {
                    string uid = chelp.RandomString(5) + Convert.ToString(new Random().Next(10, 1000)) + DateTime.Now.ToString("yyyyMMdd");

                    SetObjectPropertiesWithBaseData(custAlignRule, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(custAlignRule, uid);
                    custAlignRule.Id = sf.Post_Object("CustomerAlignmentRule__c", custAlignRule, "OCE__");
                    AddToDataMapSingleObject(custAlignRule, "customerAlignmentRuleCount");
                }
            }
        }
    }
}
