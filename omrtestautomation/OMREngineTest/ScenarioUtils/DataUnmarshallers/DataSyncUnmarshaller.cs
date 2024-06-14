using Newtonsoft.Json.Linq;
using Optimizer.Constructors;
using System;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;
using static Optimizer.Constructors.DataSyncApiRequestBody;

namespace OMREngineTest.ScenarioUtils.DataUnmarshallers.OCESyncParameterUnmarshallers
{
    class DataSyncUnmarshaller : JSONUnmarshaller
    {
        public DataSyncUnmarshaller(ScenarioContext _scenarioContext, string tconfig) : base(_scenarioContext, tconfig) { }

        public DataSyncApiRequestBody GetDataSyncRequestBody(string filePath)
        {
            {
                string[] files = GetFilesFromFilePath(filePath);
                List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
                foreach (Tuple<string, JObject> testdata in templates)
                {
                    string scenarioname = testdata.Item1;
                    JObject jsonObj = testdata.Item2;
                    DataSyncApiRequestBody datasyncRequestBody = jsonObj["DataSyncRequest"]?.ToObject<DataSyncApiRequestBody>();
                    //string regexed = SetStringValueWithBaseData(datasyncRequestBody.ToString(), baseDataMap, dataMap);
                    //datasyncRequestBody = JsonConvert.DeserializeObject<DataSyncApiRequestBody>(regexed);
                    SetObjectPropertiesWithBaseData(datasyncRequestBody, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(datasyncRequestBody, scenarioContext.Get<string>("TestUniqueIntegrationId"));
                    foreach (DataSyncFilters filters in datasyncRequestBody.Filters ?? Enumerable.Empty<DataSyncFilters>())
                    {
                        SetObjectPropertiesWithBaseData(filters, baseDataMap, dataMap);
                        SetTestUniqueIntegrationId(filters, scenarioContext.Get<string>("TestUniqueIntegrationId"));

                    }
                    foreach (DataSyncMapping mapping in datasyncRequestBody.Mapping ?? Enumerable.Empty<DataSyncMapping>())
                    {
                        SetObjectPropertiesWithBaseData(mapping, baseDataMap, dataMap);
                        SetTestUniqueIntegrationId(mapping, scenarioContext.Get<string>("TestUniqueIntegrationId"));

                        foreach (DataSyncColumns columns in mapping.Columns ?? Enumerable.Empty<DataSyncColumns>())
                        {
                            SetObjectPropertiesWithBaseData(columns, baseDataMap, dataMap);
                            SetTestUniqueIntegrationId(columns, scenarioContext.Get<string>("TestUniqueIntegrationId"));
                        }
                    }
                    return datasyncRequestBody;
                }
                return null;
            }
        }
    }
}
