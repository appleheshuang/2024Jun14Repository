using System;
using System.Net;
using Newtonsoft.Json.Linq;
using RestSharp;
using MainFramework.GlobalUtils;
using System.Threading;
using System.Collections.Generic;
using MainFramework;

namespace OMREngineTest.ScenarioUtils.Jobs
{

    public class BulkLoadJob : OptimizerJob
    {
        public string s3bucket, s3AccessKey, s3SecretKey, s3sessionToken;
        const string starttime_label = "creationDate";
        const string endtime_label = "completionDate";

        public BulkLoadJob(TenantConfig tenant_info, eToken token, string jobid = null) 
            : base(tenant_info, token, jobid)
        {
            globalCodes s3config = new globalCodes("s3TestBucket.json");
            s3bucket = t.s3bucket ?? s3config["s3Bucket"];
            s3AccessKey = s3config["awsAccessKey"];
            s3SecretKey = s3config["awsSecretKey"];

            api.done_statuses.Add(_status_code["bulkload_fail"]);
            api.done_statuses.Add(_status_code["bulkload_success"]);
            api.done_statuses.Add(_status_code["bulkload_error"]);
        }

        new public string JobStatus()
        {

            latest_status = api.Request_RestSharpValue("status", "bulkloadStatus", "GET", api_param: jobid) ?? "";
            return latest_status;
        }

        public string GetErrors()
        {
            return api.Request_RestSharpValue("errors", "bulkloadStatus", "GET", api_param: jobid);
        }

        new public int GetJobTiming()
        {
            Tuple<IRestResponse, String> response = api.Request_RestSharp("bulkloadStatus", "GET", api_param: jobid);
            try
            {
                DateTime start_time = DateTime.Parse(api.GetValuefromResponseBody(starttime_label, response.Item2));
                // per OMR-17158 - Replace with endtime from the response when this is implemented. Meanwhile use current dbtime
                // DateTime end_time = DateTime.Parse(api.GetValuefromResponseBody(endtime_label, response.Item2)); 
                DateTime end_time = new CodeHelper().GetDBTime(t.snowConnection, t.targetEnv); 
                return (int)(end_time - start_time).TotalSeconds;
            }
            catch 
            {
                Console.WriteLine("GetJobTiming: Error parsing time diff between " + starttime_label + " and now. \nResponse info=" + response.Item2);
                return -1;
            }
        }

        /// <summary>
        /// Ops Bulkload using temp s3 credentials
        /// s3 credentials, bucket and path are not required
        /// </summary>
        /// <param name="requestBody"></param>
        /// <returns>Bulkload status</returns>
        public string opsBulkLoad(JObject requestBody)
        {
            return loadTestData(requestBody);
        }

        public string doBulkLoad(string loadfilepath)
        {
            // read bulkload file
            FileHelper jfh = new FileHelper();
            JObject fileBody = jfh.loadJsonFile(loadfilepath);
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
            jBody = EnsureRequiredParams(jBody);
            if (jBody["S3BucketName"].ToString() == "" || jBody["S3BucketName"] is null) jBody["S3BucketName"] = s3bucket;
            jBody["S3AccessKey"] = s3AccessKey;
            jBody["S3SecretKey"] = s3SecretKey;
            Object reqBody = jBody;
            return loadTestData(reqBody);
        }
        JObject EnsureRequiredParams(JObject body)
        {
            if (!body.ContainsKey("S3BucketName")) body.Add("S3BucketName", s3bucket);
            if (!body.ContainsKey("S3AccessKey")) body.Add("S3AccessKey", s3AccessKey);
            if (!body.ContainsKey("S3SecretKey")) body.Add("S3SecretKey", s3SecretKey);
            return body;
        }
        public string loadTestData(object bulkBody, int intWait=poll_interval)
        {
            //Run BulkAPI
            jobid = api.Request_RestSharpValue("jobID", "bulkload", "POST", request_body: bulkBody);
            string bulkload_status = api.WaitForStatus(this.JobStatus, intWait);
            api_info = api._request + api._response;
            return bulkload_status;
        }
        public string FailMessage(string exp_status)
        {
            return "Bulkload status = " + latest_status +", Expected=" + exp_status + ",\nErrors:" + GetErrors();
        }

        public string waitSnowLoadedData(List<string> tables, QuerySnowflake sfHelp, bool unload = false, int intWait = poll_interval, int max_dur = 60)
        {
            int elapsed_time = 0;
            string jobStatus = checkSnowLoadedData(tables,sfHelp,unload);
            while (jobStatus != "OK" && elapsed_time <= max_dur)
            {
                Thread.Sleep(intWait * 1000);
                elapsed_time += intWait;
                jobStatus = checkSnowLoadedData(tables, sfHelp, unload);
            }
            return jobStatus;
        }

        public string checkSnowLoadedData(List<string> tables, QuerySnowflake sfHelp, bool unload = false)
            // returns OK if unload recCount = 0, load recCount > 0
        {
            string snow_schema = "STATIC";
            var strReturn = "Snow => ";
            var isLoaded = "OK";

            foreach (var sfTable in tables)
            {
                strReturn += sfTable + " : ";
                int recCount = int.Parse(sfHelp.ReturnSingleColumnValueFromSnowFlake("select count(*) \"recs\" from \""+snow_schema+"\".\""+sfTable+"\";","recs"));
                if ((recCount == 0) != unload) isLoaded = "NOT OK";
                strReturn += sfTable + " : " + recCount.ToString() + ";\r\n";
            }

            //in case we need to check per table strReturn is ready
            if (isLoaded == "NOT OK")
            {
                return strReturn;
            }
            else
            {
                return isLoaded;
            }
        }

        /// <summary>
        /// Request s3 temp credentials from the engine
        /// Collect the bucket, access + secret key, session token
        /// Derive the relative s3 path from the full path (includes bucket)
        /// </summary>
        /// <returns>s3 path to use in the s3 client put request</returns>
        public string GetTempS3Path()
        {
            // Get temp credentials token from the engine
            Tuple<IRestResponse, String> token_response = api.Request_RestSharp("tempAwsCredentials", "GET");
            if (token_response.Item1.StatusCode == HttpStatusCode.OK)
            {
                // Capture the s3 session values returned
                string responseBody = token_response.Item2;
                s3bucket = api.GetValuefromResponseBody("bucket", responseBody);
                s3AccessKey = api.GetValuefromResponseBody("awsAccessKeyId", responseBody);
                s3SecretKey = api.GetValuefromResponseBody("awsSecretAccessKey", responseBody);
                s3sessionToken = api.GetValuefromResponseBody("awsSessionToken", responseBody);
                //string expiryDate = api.GetValuefromResponseBody("expiryDate", responseBody);
                string s3Path = api.GetValuefromResponseBody("s3Path", responseBody);
                return s3Path.Replace("s3://" + s3bucket + "/","");
            }
            else
                throw new Exception("Failed to get temp s3 credentials \nRequest: " + api._request + "\nResponse: " + api._response);
        }

    }
}