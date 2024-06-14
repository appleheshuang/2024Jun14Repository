using System;
using System.Threading.Tasks;
using Xunit.Abstractions;
using OMREngineTest;
using Sailfish.RealignmentEngine.Tests;
using Newtonsoft.Json.Linq;
using Sailfish.RealignmentEngine.Scenario;

namespace MainFramework.GlobalUtils
{
    public class CompareFiles
    {
        private readonly ITestOutputHelper _output;

        public CompareFiles(ITestOutputHelper output)
        {
            _output = output;
        }

        public void QuerySnowFlakeCompareUploadS3(String SnowFlakeSQL, String fileName, String Uniqkey, string snowdb)
        {
            FileHelper jfh = new FileHelper();
            var config = new DevConfig();

            jfh.generateJsonFile(SnowFlakeSQL, fileName, "EngineTestResults/CompareData/Actual/", Uniqkey, snowdb);

            ////Compare two Json files
            JObject ActualJObject = jfh.loadJsonFile(jfh.GetFilePath("OMRegionActual.xml", "EngineTestResults/CompareData/Actual/"));
            JObject ExpectedJObject = jfh.loadJsonFile(jfh.GetFilePath("OMRegionExpected.xml", "EngineTestResults/CompareData/Expected/"));

            String Diff = jfh.CompareObjects(ExpectedJObject, ActualJObject).ToString();

            //Export Diff
            jfh.generateTextFile("OMRegionDiff.txt", Diff, "EngineTestResults/CompareData/Actual");

            //////get timestamp
            //String timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            ////Upload ActualResult and Compare Diff to S3
            //S3Storage s3 = new S3Storage(config["bucketName"], config["accessKey"], config["secretKey"], config["region"]);
            //await s3.UploadDirectory(config["root"] + "/" + timeStamp, jfh.GetDirectoryPath("EngineTestResults/CompareData/Actual"));

            //_output.WriteLine(FileHelper.CompareResult);
        }
    }
}
