using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TechTalk.SpecFlow;
using System.Text.RegularExpressions;

namespace OMREngineTest.ScenarioUtils
{
    class MergeUnmarshaller : JSONUnmarshaller
    {
        struct WinLosePair
        {
            public string WinnerId;
            public string LoserId;
        }
        public MergeUnmarshaller(ScenarioContext _scenarioContext, string tconfig, string testid = null)
            : base(_scenarioContext, tconfig, testid) { }

        /// <summary>
        /// Return a JArray of Winner-Loser pairs from the file,
        /// with base data and testid references resolved
        /// </summary>
        /// <param name="filePath">Json file containing merge request body</param>
        /// <returns>Request body as JArray</returns>
        public JArray GetMergeBody(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            Tuple<string, JObject> testdata = templates[0];
            JObject jsonObj = testdata.Item2;
            SetBaseDataMap(jsonObj);
            string mergeString;
            try
            {
                mergeString = jsonObj["Merge"].ToString(); 
            }
            catch (Exception e)
            {
                Console.WriteLine("Account Merge file does not contain 'Merge' node of JArray");
                throw e;
            }
            SetFieldProperties(mergeString, baseDataMap, dataMap);
            mergeString = Regex.Replace(mergeString, "{{TestUniqueIntegrationId}}", testId, RegexOptions.IgnoreCase);
            return JArray.Parse(mergeString);
        }

    }
}
