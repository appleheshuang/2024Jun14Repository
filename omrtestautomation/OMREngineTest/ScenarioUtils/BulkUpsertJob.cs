using System;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Sailfish.RealignmentEngine.Salesforce
{
    public class DataLoad : IDisposable
    {
        private ISalesforceRestAPI _restAPI;
        public String Id { get; set; }
        public String Operation { get; set; }
        public String Object { get; set; }
        public String CreatedById { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime SystemModStamp { get; set; }
        public String State { get; set; }
        public String ExternalIdFieldName { get; set; }
        public String ConcurrencyMode { get; set; }
        public String ContentType { get; set; }
        public String ApiVersion { get; set; }
        public int NumberRecordsProcessed { get; set; }
        public int NumberRecordsFailed { get; set; }

        public bool Completed
        {
            get
            {
                return State == "JobComplete" || State == "Aborted";
            }
        }

        public DataLoad(ISalesforceRestAPI restAPI, string objectName, string externalFieldName, Stream stream)
        {
            _restAPI = restAPI;

            string url = string.Format("{0}/jobs/ingest", _restAPI.DataService);
            string contentType = "application/json; charset=UTF-8";
            string method = "POST";
            string body = $@"{{
                            ""object"":""{objectName}"",
                            ""externalIdFieldName"":""{externalFieldName}"",
                            ""contentType"":""CSV"",
                            ""operation"":""upsert"", 
                            ""lineEnding"":""LF""
                            }}";
            byte[] bytes = Encoding.ASCII.GetBytes(body);
            string response = _restAPI.Invoke(url, method, contentType, bytes);
            ParseJobInfo(response);

            url = string.Format("{0}/jobs/ingest/{1}/batches", _restAPI.DataService, Id);
            contentType = "text/csv";
            method = "PUT";
            byte[] fileBytes= new byte[stream.Length];
            stream.Read(fileBytes, 0, fileBytes.Length);
            response = _restAPI.Invoke(url, method, contentType, fileBytes);

            url = string.Format("{0}/jobs/ingest/{1}/", _restAPI.DataService, Id);
            contentType = "application/json; charset=UTF-8";
            method = "PATCH";
            body = $@"{{""state"":""UploadComplete""}}";
            bytes = Encoding.ASCII.GetBytes(body);
            response = _restAPI.Invoke(url, method, contentType, bytes);
            ParseJobInfo(response);
        }

        public void Refresh()
        {
            string url = string.Format("{0}/jobs/ingest/{1}/", _restAPI.DataService, Id);
            string contentType = "application/json; charset=UTF-8";
            string method = "GET";
            string response = _restAPI.Invoke(url, method, contentType);
            ParseJobInfo(response);
        }

        public string SuccessfulResults()
        {
            string url = string.Format("{0}/jobs/ingest/{1}/successfulResults/", _restAPI.DataService, Id);
            string contentType = "application/json; charset=UTF-8";
            string method = "GET";
            string response = _restAPI.Invoke(url, method, contentType);
            return response;
        }

        public string FailedResults()
        {
            string url = string.Format("{0}/jobs/ingest/{1}/failedResults/", _restAPI.DataService, Id);
            string contentType = "application/json; charset=UTF-8";
            string method = "GET";
            string response = _restAPI.Invoke(url, method, contentType);
            return response;
        }

        public void Dispose()
        {
            string url = string.Format("{0}/jobs/ingest/{1}/", _restAPI.DataService, Id);
            string contentType = "application/json; charset=UTF-8";
            string method = "DELETE";
            string response = _restAPI.Invoke(url, method, contentType);
        }

        private void ParseJobInfo(String response)
        {
            JObject json = JObject.Parse(response);
            
            Id = json["id"].ToString();
            Operation = json["operation"].ToString();
            Object = json["object"].ToString();
            CreatedById = json["createdById"].ToString();
            CreatedDate = DateTime.Parse(json["createdDate"].ToString());
            SystemModStamp = DateTime.Parse(json["systemModstamp"].ToString());
            State = json["state"].ToString();
            ExternalIdFieldName = json["externalIdFieldName"].ToString();
            ConcurrencyMode = json["concurrencyMode"].ToString();
            ContentType = json["contentType"].ToString();
            ApiVersion = json["apiVersion"].ToString();
            NumberRecordsProcessed = json["numberRecordsProcessed"]?.ToObject<int>() ?? 0;
            NumberRecordsFailed = json["numberRecordsFailed"]?.ToObject<int>() ?? 0;
        }
    }
}