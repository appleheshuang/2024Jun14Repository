using Newtonsoft.Json.Linq;
using OMREngineTest.Constructors;
using System;
using System.Collections.Generic;
using System.Text;
using TechTalk.SpecFlow;

namespace OMREngineTest.ScenarioUtils
{
    class RootCauseJSONUnmarshaller : JSONUnmarshaller
    {

        public RootCauseJSONUnmarshaller(ScenarioContext scenarioContext, string tconfig) : base(scenarioContext, tconfig) { }
        public RootCauseApiRequestBody GetRequestBodyOnly(string filePath)
        {

            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                var rootCauseRequestBody = jsonObj["RootCauseRequest"]?.ToObject<RootCauseApiRequestBody>();
                SetObjectPropertiesWithBaseData(rootCauseRequestBody, baseDataMap, dataMap);
                return rootCauseRequestBody;
            }
            return null;
        }

        public RootCauseApiResponseBody GetResponseBodyTestsOnly(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                var rootCauseTests = jsonObj["RootCauseResponse"]?.ToObject<RootCauseApiResponseBody>();
                SetObjectPropertiesWithBaseData(rootCauseTests, baseDataMap, dataMap);

                foreach (RootCauseRule rule in rootCauseTests.rules)
                {
                    foreach (RootCauseData data in rule.data)
                    {
                        SetObjectPropertiesWithBaseData(data, baseDataMap, dataMap);
                    }
                    SetObjectPropertiesWithBaseData(rule, baseDataMap, dataMap);
                    rule.effectiveCalculationDate = DateTime.Now.ToString("yyyy-MM-dd");
                    rule.endCalculationDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
                return rootCauseTests;
            }
            return null;
        }

    }
}
