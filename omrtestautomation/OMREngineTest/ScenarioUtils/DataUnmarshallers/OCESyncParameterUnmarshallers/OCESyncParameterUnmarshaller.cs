using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMREngineTest.ScenarioUtils.Jobs;
using OMREngineTest.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using TechTalk.SpecFlow;

namespace OMREngineTest.ScenarioUtils.DataUnmarshallers.OCESyncParameterUnmarshallers
{
    class OCESyncParameterUnmarshaller : JSONUnmarshaller
    {
        public OCESyncParameterUnmarshaller(ScenarioContext _scenarioContext, string tconfig) : base(_scenarioContext, tconfig) { }

        public OCESyncJobRequestBody GetOCESyncJobParameters(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                OCESyncJobRequestBody ocesyncPublishOtherDataParams = JsonConvert.DeserializeObject<OCESyncJobRequestBody>(jsonObj["OCESyncParams"].ToString(), new JsonSerializerSettings()
                {
                    DateParseHandling = DateParseHandling.None
                });
                //var ocesyncPublishOtherDataParams = jsonObj["OCESyncParams"]?.ToObject<IOCESyncJobParameters>();
                SetObjectPropertiesWithBaseData(ocesyncPublishOtherDataParams, baseDataMap, dataMap);
                return ocesyncPublishOtherDataParams;
            }
            return null;
        }
    }
}
