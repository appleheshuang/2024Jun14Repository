using MainFramework;
using MainFramework.GlobalUtils;
using OMREngineTest.Utils;
using System.Net;
using System;
using System.Collections.Generic;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json.Linq;
using System.Linq;
using OMREngineTest.ScenarioUtils.Jobs;
using System.IO;
using Optimizer.Constructors.ApiResponseObjects;
using Newtonsoft.Json;

namespace OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions
{
    [Binding]
    [Collection("Smoketests")]
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]

    public class BulkLoadSteps : BaseEngineSteps
    {
        protected Dictionary<string, dynamic> baseDataMap;
        protected Dictionary<string, dynamic> dataMap;
        private string s3bucket;
        private const string initialBulkLoad = "Smoketest\\LoadData\\InitialBulkload.json";
        private BulkLoadJob bulkJob;
        private const string TempFileRoot = "TestData/TempFiles/";

        public BulkLoadSteps(ScenarioContext scenarioContext, ITestOutputHelper output) : base(scenarioContext, output)
        {
            if (!_scenarioContext.TryGetValue("baseDataMap", out baseDataMap))
            {
                baseDataMap = new Dictionary<string, dynamic>();
            }
            if (!_scenarioContext.TryGetValue("dynamicDataMap", out dataMap))
            {
                dataMap = new Dictionary<string, dynamic>();
            }
            bulkJob = new BulkLoadJob(_engine.t, _engine.engine_token);
        }

        /// <summary>
        /// Initial bulk load sets up reference data for all test. First check if data needs loading.
        /// Invoked in the Setup stage
        /// </summary>
        [Then(@"I bulkload and sync initial data")]
        public void GivenIBulkloadAndSyncAllForOrg()
        {
            ThenIBulkloadAndSyncInitialData(initialBulkLoad);
        }
        [Then(@"I bulkload and sync initial '(.*)' data")]
        public void ThenIBulkloadAndSyncInitialData(string bulkload_file)
        {
            if (_engine.RunDataTests(_scenarioContext, bulkload_file, suppress_logging: true) != "OK")
            {
                GivenIBulkloadAndSyncAllFromFile(bulkload_file);
                Assert.Equal("OK", _engine.RunDataTests(_scenarioContext, bulkload_file));
            }
        }

        [Given(@"I bulkload from the bulkload settings in '(.*)' and trigger post-migration push")]
        public void GivenIBulkloadAndSyncAllFromFile(string file)
        {
            GivenIBulkloadFromFile(file);
            ThenISyncScope("all");
        }
        [Then(@"the sync is (.*) within (.*) min")]
        public void ThenTheSyncStatusIsWithinMin(string required_state, int max_wait)
        {
            if (_scenarioContext.ContainsKey("syncJob"))
            {
                SyncJob sync = _scenarioContext.Get<SyncJob>("syncJob");
                switch (required_state) {
                    case "successful": JobStatusCheck(sync.IsSuccessful(max_wait * 60), sync.FailMessage()); break;
                    case "completed": JobStatusCheck(sync.HasCompleted(max_wait * 60), sync.FailMessage()); break;
                    case "failed": JobStatusCheck(sync.HasFailed(max_wait * 60), sync.FailMessage()); break;
                    default: JobStatusCheck(sync.HasCompleted(max_wait * 60), sync.FailMessage()); break;
                }
            }
        }

        [Given(@"I bulkload from the bulkload settings in '(.*)'")]
        public void GivenIBulkloadFromFile(string file)
        {
            string exp_status = BulkLoadJob._status_code["bulkload_success"];
            string filepath = new FileHelper().GetFilePath(file, TestDataRoot);
            JobStatusCheck(exp_status == bulkJob.doBulkLoad(filepath), bulkJob.FailMessage(exp_status), bulkJob.api_info);
            _scenarioContext.Set<bool>(true, "bulkloaded");
            ThenTheBulkloadIsSuccessful();
        }

        [Given(@"I submit the bulkload '(.*)'")]
        public void GivenISubmitBulkloadFromFile(string file)
        {
            string filepath = new FileHelper().GetFilePath(file, TestDataRoot);
            bulkJob.doBulkLoad(filepath);
        }

        [Given(@"I patch (.*) to '(.*)' for (.*) '(.*)'")]
        public void GivenIPatch_WithTo(string setcol, string newvalue, string entity, string idvalue)
        {
            HttpStatusCode patchresponce = _engine.odataq.PatchSingleValue(entity, idvalue, setcol, newvalue);
            Assert.True(patchresponce == HttpStatusCode.OK, "Patch " + entity + " (" + newvalue + ") failed: " + patchresponce);
        }


        [Given(@"I purge existing data")]
        public void GivenIPurgeExistingData()
        {
            _engine.PreExistingData(reset_data: true);
        }

        /// <summary>
        /// I bulkload unique - Includes submit and check success, show errors, in one step
        /// </summary>
        /// <param name="bulkloadfile"></param>
        [Given(@"I bulkload unique '(.*)'")]
        public void GivenIBulkloadUnique(string bulkloadfile)
        {
            FileHelper jfh = new FileHelper();
            List<string> values = NewFileWithReplacedUniqueId(bulkloadfile);
            // do the bulkload
            string exp_status = BulkLoadJob._status_code["bulkload_success"];
            if (bulkJob.doBulkLoad(values[1]) == exp_status)
            {
                PurgeUniqueFromS3(values[1]);
                jfh.deletefile(values[0], values[2]);
            }
            else
                JobStatusCheck(false, bulkJob.FailMessage(exp_status), bulkJob.api_info);
        }

        [Given(@"I submit a unique bulkload request from file '(.*)'")]
        public void GivenISubmitAUniqueBulkloadRequestFromFile(string bulkloadfile)
        {
            FileHelper jfh = new FileHelper();
            List<string> values = NewFileWithReplacedUniqueId(bulkloadfile);
            // do the bulkload
            bulkJob.doBulkLoad(values[1]);
            PurgeUniqueFromS3(values[1]);
            jfh.deletefile(values[0], values[2]);
        }

        [Then(@"The bulkload is successful")]
        public void ThenTheBulkloadIsSuccessful()
        {
            bool bulk_success = bulkJob.latest_status == EngineTest._status_code["bulkload_success"];
            JobStatusCheck(bulk_success, "Bulkload failed with status=" + bulkJob.latest_status, bulkJob.api_info);
            if (bulk_success)
                _scenarioContext.Set<bool>(true, "bulkloaded");
        }
        [Then(@"I sync (.*)")]
        public void ThenISyncScope(string sync_scope = "latest")
        {
            SyncJob sync = new SyncJob(_engine.t, _engine.engine_token, id: null);
            bool syncall = sync_scope.ToLower() == "all";
            Assert.Equal("OK", sync.Sync(syncall,_engine.PreExistingData()));
            _scenarioContext.Set<SyncJob>(sync, "syncJob");
        }

        [Then(@"The bulkload fails")]
        public void ThenTheBulkloadFails()
        {
            bool bulk_failure = bulkJob.latest_status == EngineTest._status_code["bulkload_fail"];
            JobStatusCheck(bulk_failure, "Unexpected Bulkload status=" + bulkJob.latest_status, bulkJob.api_info);
            if (bulk_failure)
                _scenarioContext.Set<bool>(false, "bulkloaded");
        }

        [Then(@"The bulkload has errors per '(.*)'")]
        public void ThenTheBulkloadHasErrorsIn(string file)
        {
            Assert.Equal(EngineTest._status_code["bulkload_error"], bulkJob.latest_status);
            _scenarioContext.Set<bool>(false, "bulkloaded");
            FileHelper jfh = new FileHelper();

            string id = _scenarioContext.Get<string>("TestUniqueIntegrationId");

            string jsonString = File.ReadAllText(jfh.GetFilePath(file, TestDataRoot));
            string replacedString = "";
            if (jsonString.Contains("{{TestUniqueIntegrationId}}"))
            {
                replacedString = jsonString.Replace("{{TestUniqueIntegrationId}}", id);
            }
            else
            {
                replacedString = jsonString;
            }

            // Convert the JSON string to a JObject:
            JObject errorBody = Newtonsoft.Json.JsonConvert.DeserializeObject(replacedString) as JObject;

            List<BulkloadError> expected = JsonConvert.DeserializeObject<List<BulkloadError>>(errorBody["Errors"].ToString());
            List<BulkloadError> actual = JsonConvert.DeserializeObject<List<BulkloadError>>(bulkJob.GetErrors());

            Assert.Equal(JsonConvert.SerializeObject(expected.OrderBy(e => e.error)), JsonConvert.SerializeObject(actual.OrderBy(e => e.error)));
        }

        [Given(@"I trigger a failing bulkload unique values from the bulkload settings in '(.*)' with Error Messages in '(.*)'")]
        public void GivenITriggerAFailingBulkloadUniqueValuesFromTheBulkloadSettingsInWithErrorMessagesIn(string bulkloadFile, string errorMessagesFile)
        {

            List<string> values = NewFileWithReplacedUniqueId(bulkloadFile);
            string id = _scenarioContext.Get<string>("TestUniqueIntegrationId");

            string jsonString = File.ReadAllText(values[1]);
            string replacedString = "";
            if (jsonString.Contains("{{TestUniqueIntegrationId}}"))
            {
                replacedString = jsonString.Replace("{{TestUniqueIntegrationId}}", id);
            }
            else
            {
                replacedString = jsonString;
            }

            // Convert the JSON string to a JObject:
            JObject jObject = Newtonsoft.Json.JsonConvert.DeserializeObject(replacedString) as JObject;
            string updatedJsonString = jObject.ToString();
            File.WriteAllText(values[1], updatedJsonString);

            GivenISubmitAUniqueBulkloadRequestFromFile(values[2]);
            ThenTheBulkloadHasErrorsIn(values[1]);
            _scenarioContext.Set<bool>(false, "bulkloaded");
        }

        protected List<string> NewFileWithReplacedUniqueId(string bulkloadfile)
        {
            // read bulkload file
            FileHelper jfh = new FileHelper();
            string jsonfilepath = jfh.GetFilePath(bulkloadfile, TestDataRoot);

            // create new files with replaced {{TestUniqueIntegrationId}}
            JObject fileBody = jfh.loadJsonFile(jsonfilepath);
            JObject jBody;
            try
            {
                jBody = JObject.Parse(fileBody["Bulkload"].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Bulkload file does not contain 'Bulkload' node");
                throw e;
            }
            JArray LoadFiles = JArray.Parse(jBody["Files"].ToString());
            string s3filedir = jBody["S3Path"].ToString();
            s3bucket = new BulkLoadJob(_engine.t, _engine.engine_token).s3bucket;
            if (jBody.ContainsKey("S3BucketName") && jBody["S3BucketName"].ToString() != "")
                s3bucket = jBody["S3BucketName"].ToString();

            S3Client s3help = new S3Client();
            CodeHelper chelp = new CodeHelper();
            string uid;
            try
            {
                uid = _scenarioContext.Get<string>("TestUniqueIntegrationId");
            }
            catch (Exception e)
            {
                Console.WriteLine("You must set the TestId first using \"Given a unique id for the test\"");
                throw e;
            }
            string new_datapath = s3help.normalized_path(s3filedir) + uid;


            List<string> load_files = ((JArray)LoadFiles).Select(e => (string)e["file"]).ToList();
            foreach (string loadfile in load_files)
            {
                Tuple<bool, string> file_data = s3help.getFileContents(loadfile, s3filedir, s3bucket);
                if (file_data.Item1)
                {
                    string newData = jfh.SetFieldProperties(file_data.Item2, baseDataMap, dataMap);
                    string new_data = newData.Replace("{{TestUniqueIntegrationId}}", uid);
                    s3help.putFileContents(loadfile, new_data, new_datapath, s3bucket);
                }
                else
                    throw s3help.ConfigNotFoundException("BulkLoad file not found: " + loadfile);
            }

            // Update the json file to use the new s3 path
            jBody["S3Path"] = new_datapath + '/';
            fileBody["Bulkload"] = jBody;

            // Create new bulkload json in TempFiles directory
            FileAndPath bulkfilepath = new FileAndPath(bulkloadfile);
            string new_jsonfilename = bulkfilepath.file.Replace(".json", "_" + uid + ".json");
            string tempfileroot = jfh.GetDirectoryPath(TempFileRoot);
            string new_jsonfile = jfh.writeFilePath(tempfileroot + new_jsonfilename, fileBody.ToString());

            FileAndPath newfile = new FileAndPath(new_jsonfile);
            return new List<string>() { newfile.path, new_jsonfile, newfile.file };
        }
        
        protected bool PurgeUniqueFromS3(string bulkloadfilepath)
        {
            // read bulkload file
            JObject fileBody = new FileHelper().loadJsonFile(bulkloadfilepath);
            string s3filedir = JObject.Parse(fileBody["Bulkload"].ToString())["S3Path"].ToString();
            return new S3Client().purgeDirectory(s3filedir, s3bucket);
        }

        [Given(@"the s3 bucket for '(.*)'")]
        public void GivenTheS3BucketFor(string targetEnv)
        {
            s3bucket = new EnvFixture("envConfig-" + targetEnv + ".json").Config.TestDataBucket;
        }
        [Given(@"I cleanup unique bulkload files for '(.*)'")]
        public void GivenICleanupUniqueBulkloadFilesFor(string s3FeatureDir)
        {
            S3Client s3 = new S3Client(); 
            string[] contents = s3.getDirContents(s3FeatureDir, s3bucket);
            // check each
            foreach (string f in contents)
            {
                FileAndPath this_item = new FileAndPath(f);
                FileAndPath baseline_item = new FileAndPath(this_item.file, s3FeatureDir);
                if (this_item.path != baseline_item.path)
                    s3.purgeItem(f, s3bucket);
            }
        }

    }
}
