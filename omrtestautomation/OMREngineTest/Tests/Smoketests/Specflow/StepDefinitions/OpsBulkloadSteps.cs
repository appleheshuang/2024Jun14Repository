using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using MainFramework.GlobalUtils;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using OMREngineTest.Utils;
using OMREngineTest.ScenarioUtils.Jobs;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    [Collection("Smoketests")]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]

    public class OpsBulkLoadSteps : BaseEngineSteps
    {
        protected Dictionary<string, dynamic> baseDataMap;
        protected Dictionary<string, dynamic> dataMap;
        private BulkLoadJob bulkJob;
        private const string bulkloadSettingsDefault = "BulkLoad/DefaultSettings.json"; 
        
        JObject loadRequestBody;
        FileHelper jfh;
        string s3dataPath, testUId, testPath;
        List<fileTableData> tableData;
        struct fileTableData
        {
            public string table;
            public string file;
            public string data;
            public fileTableData(string tableName, string filePath, string fileData)
            {
                table = tableName; file = filePath; data = fileData;
            }
        }

        public OpsBulkLoadSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
            jfh = new FileHelper();
            if (!_scenarioContext.TryGetValue("baseDataMap", out baseDataMap))
            {
                baseDataMap = new Dictionary<string, dynamic>();
            }
            if (!_scenarioContext.TryGetValue("dynamicDataMap", out dataMap))
            {
                dataMap = new Dictionary<string, dynamic>();
            }
            bulkJob = new BulkLoadJob(_engine.t, _engine.engine_token);
            testPath = UniqueData("TestRun_{{TestUniqueIntegrationId}}/");
        }

        [Given(@"the bulkload settings '(.*)'")]
        public void GivenTheBulkloadSettings(string bulkloadsettings)
        {
            // read bulkload settings
            string jsonfilepath = jfh.GetFilePath(bulkloadsettings, TestDataRoot);

            JObject settingsFileBody = jfh.loadJsonFile(jsonfilepath);
            try
            {
                loadRequestBody = JObject.Parse(settingsFileBody["Bulkload"].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Bulkload file does not contain 'Bulkload' node");
                throw e;
            }
        }
        public void DefaultBulkloadSettings()
        {
            string jsonfilepath = jfh.GetFilePath(bulkloadSettingsDefault, "TestData");
            JObject settingsFileBody = jfh.loadJsonFile(jsonfilepath);
            loadRequestBody = JObject.Parse(settingsFileBody["Bulkload"].ToString());
        }

        /// <summary>
        /// Given the datafile for each table to load,
        /// Make the substitutions and create the unique data in the fileTableData objects
        /// Prepare the files node for the bulkload request body
        /// </summary>
        /// <param name="type">"unique" if substitution is wanted </param>
        /// <param name="t">Table having the fable + file</param>
        [Given(@"(.*) data for each table")]
        public void GivenTheDataForEachTable(string type,Table t)
        {
            JArray fileArray = new JArray();
            tableData = new List<fileTableData> { };
            foreach (var tableItem in t.Rows)
            {
                string tableName = tableItem[0];
                string fileName = tableItem[1];
                string localFilePath = jfh.GetFilePath(fileName, TestDataRoot);
                string fileData = File.ReadAllText(localFilePath);
                if (type.ToLower() == "unique") fileData = UniqueData(fileData);
                tableData.Add(new fileTableData(tableName, fileName, fileData));
                var thisJFile = JObject.Parse("{\"file\": \""+ testPath + fileName + "\", \"table\": \"" + tableName + "\"}");
                fileArray.Add(thisJFile);
            }
            loadRequestBody["Files"] = fileArray;
        }

        /// <summary>
        /// Given the directory of the files to load,
        /// Make the substitutions and create the unique data in the fileTableData objects
        /// Prepare the files node for the bulkload request body
        /// </summary>
        /// <param name="type">"unique" if substitution is wanted </param>
        /// <param name="dir">Directory of the data to load (files named per the table) </param>
        [Given(@"(.*) data from the directory '(.*)'")]
        public void GivenUniqueDataFromTheDirectory(string type, string dir)
        {
            string origDataPath = jfh.GetDirectoryPath(dir, TestDataRoot);
            JArray fileArray = new JArray();
            tableData = new List<fileTableData> { };
            string[] load_files = Directory.GetFiles(origDataPath);
            foreach (string loadfile in load_files)
            {
                string fileData = File.ReadAllText(@loadfile);
                if (type.ToLower() == "unique") fileData = UniqueData(fileData);
                string fileName = new FileAndPath(loadfile).file;
                string tableName = fileName.Substring(0, fileName.Length - 4);
                tableData.Add(new fileTableData(tableName, fileName, fileData));
                var thisJFile = JObject.Parse("{\"file\": \"" + testPath + fileName + "\", \"table\": \"" + tableName + "\"}");
                fileArray.Add(thisJFile);
            }
            loadRequestBody["Files"] = fileArray;
        }

        void GivenUniqueDataFromTheFile(string type, string file)
        {
            string loadfile = jfh.GetFilePath(file, TestDataRoot);
            string fileData = File.ReadAllText(@loadfile);
            if (type.ToLower() == "unique") fileData = UniqueData(fileData);
            string fileName = new FileAndPath(loadfile).file;
            string tableName = fileName.Substring(0, fileName.Length - 4);
            tableData = new List<fileTableData> { new fileTableData(tableName, fileName, fileData) };
            var thisJFile = JObject.Parse("{\"file\": \"" + testPath + fileName + "\", \"table\": \"" + tableName + "\"}");
            loadRequestBody["Files"] = new JArray(thisJFile);
        }

        string UniqueData(string data)
        {
            if (testUId == null || testUId == "")
                try
                {
                    testUId = _scenarioContext.Get<string>("TestUniqueIntegrationId");
                }
                catch (Exception e)
                {
                    Console.WriteLine("You must set the TestId first using \"Given a unique id for the test\"");
                    throw e;
                }
            string newData = jfh.SetFieldProperties(data, baseDataMap, dataMap);
            return newData.Replace("{{TestUniqueIntegrationId}}", testUId);
        }

        [When(@"I get temp aws credentials")]
        public void WhenIGetTempSCredentials()
        {
            s3dataPath = bulkJob.GetTempS3Path();
        }

        /// <summary>
        /// Upload the data for the list of files, prepared in the tableData
        /// </summary>
        [When(@"I upload the data to s3")]
        public void WhenIUploadTheDataToS3()
        {
            S3Client s3help = new S3Client(accessKey: bulkJob.s3AccessKey, secretKey: bulkJob.s3SecretKey, sessionToken: bulkJob.s3sessionToken);
            foreach (fileTableData uploadTable in tableData) 
            { 
                s3help.putFileContents(uploadTable.file, uploadTable.data, s3dataPath + testPath, bulkJob.s3bucket);
            }
        }

        /// <summary>
        /// Bulkload and check the job status
        /// </summary>
        [Then(@"I bulkload using the temp credentials")]
        public void ThenIBulkloadUsingTheTempCredentials()
        {
            string loadStatus = bulkJob.opsBulkLoad(loadRequestBody);
            bool bulk_success = loadStatus == EngineTest._status_code["bulkload_success"];
            JobStatusCheck(bulk_success, "Bulkload failed with status=" + loadStatus, bulkJob.api_info);
        }

        /// <summary>
        /// Bulkload using temp credentials in one step
        /// Use the default bulkload settings, if not already set
        /// Can be used with a json file (must match an OMR table name),
        ///  or a directory (containing files which match an OMR table).
        /// </summary>
        /// <param name="dataPath">File or directory path within test data</param>
        [Given(@"I bukload '(.*)' using temp credentials")]
        public void GivenIBukloadUsingTempCredentials(string dataPath)
        {
            if (loadRequestBody == null)
                DefaultBulkloadSettings();
            if (dataPath.EndsWith(".json") || dataPath.EndsWith(".csv"))
                GivenUniqueDataFromTheFile("unique", dataPath);
            else
                GivenUniqueDataFromTheDirectory("unique", dataPath);
            WhenIGetTempSCredentials();
            WhenIUploadTheDataToS3();
            ThenIBulkloadUsingTheTempCredentials();
            ThenPurgeDataFromS3();
        }

        /// <summary>
        /// Delete all files from the test data path in s3
        /// </summary>
        [Then(@"purge the data from s3")]
        public void ThenPurgeDataFromS3()
        {
            S3Client s3help = new S3Client(accessKey: bulkJob.s3AccessKey, secretKey: bulkJob.s3SecretKey, sessionToken: bulkJob.s3sessionToken);
            s3help.purgeDirectory(s3dataPath + testPath, bulkJob.s3bucket);
        }

    }
}
