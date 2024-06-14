using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Sailfish.RealignmentEngine.Salesforce
{
    public class BulkQueryJob : IDisposable
    {
        private ISalesforceRestAPI _restAPI;
        public String Id { get; set; }
        public String Operation { get; set; }
        public String Object { get; set; }
        public String CreatedById { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime SystemModStamp { get; set; }
        public String State { get; set; }
        public String ConcurrencyMode { get; set; }
        public String ContentType { get; set; }
        public int NumberBatchesQueued { get; set; }
        public int NumberBatchesInProgress { get; set; }
        public int NumberBatchesCompleted { get; set; }
        public int NumberBatchesFailed { get; set; }
        public int NumberBatchesTotal { get; set; }
        public int NumberRecordsProcessed { get; set; }
        public int NumberRecordsFailed { get; set; }
        public int NumberRetries { get; set; }
        public int TotalProcessingTime { get; set; }
        public int ApiActiveProcessingTime { get; set; }
        public int ApexProcessingTime { get; set; }

        public bool Completed
        {
            get
            {
                return NumberBatchesTotal == (NumberBatchesCompleted + NumberBatchesFailed) ||
                       State == "Aborted";
            }
        }

        public BulkQueryJob(string salesforceObject, ISalesforceRestAPI restAPI)
        {
            _restAPI = restAPI;
            string url = string.Format("{0}/job", _restAPI.AsyncService);
            string contentType = "application/xml; charset=UTF-8";
            string method = "POST";
            string body = string.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
                                <jobInfo xmlns=""http://www.force.com/2009/06/asyncapi/dataload"">
                                <operation>{0}</operation>
                                <object>{1}</object>
                                <contentType>{2}</contentType>
                            </jobInfo>", "query", salesforceObject, "CSV");
            byte[] bytes = Encoding.ASCII.GetBytes(body);
            string response = _restAPI.Invoke(url, method, contentType, bytes);
            ParseJobInfo(response);
        }

        public void Refresh()
        {
            string url = string.Format("{0}/job/{1}", _restAPI.AsyncService, Id);
            string contentType = "text/csv; charset=UTF-8";
            string method = "GET";
            string response = _restAPI.Invoke(url, method, contentType);
            ParseJobInfo(response);
        }

        public Batch CreateBatch(string query)
        {
            Batch batch = new Batch(Id, query, _restAPI);
            return batch;
        }

        public void Close()
        {
            string url = string.Format("{0}/job/{1}", _restAPI.AsyncService, Id);
            string contentType = "application/xml; charset=UTF-8";
            string method = "POST";

            string body = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <jobInfo xmlns=""http://www.force.com/2009/06/asyncapi/dataload"">
                            <state>Closed</state>
                            </jobInfo>";
            byte[] bytes = Encoding.ASCII.GetBytes(body);
            string response = _restAPI.Invoke(url, method, contentType, bytes);
            ParseJobInfo(response);
        }

        public void Dispose()
        {
            Close();
        }

        private void ParseJobInfo(String jobXml)
        {
            XDocument doc = XDocument.Parse(jobXml);

            XElement jobInfoElement = doc.Root;
            if (jobInfoElement != null)
            {
                List<XElement> jobInfoChildElements = jobInfoElement.Elements().ToList();

                foreach (XElement e in jobInfoChildElements)
                {
                    switch (e.Name.LocalName)
                    {
                        case "id":
                            Id = e.Value;
                            break;
                        case "operation":
                            Operation = e.Value;
                            break;
                        case "object":
                            Object = e.Value;
                            break;
                        case "createdById":
                            CreatedById = e.Value;
                            break;
                        case "createdDate":
                            CreatedDate = DateTime.Parse(e.Value);
                            break;
                        case "systemModstamp":
                            SystemModStamp = DateTime.Parse(e.Value);
                            break;
                        case "state":
                            State = e.Value;
                            break;
                        case "concurrencyMode":
                            ConcurrencyMode = e.Value;
                            break;
                        case "contentType":
                            ContentType = e.Value;
                            break;
                        case "numberBatchesQueued":
                            NumberBatchesQueued = int.Parse(e.Value);
                            break;
                        case "numberBatchesInProgress":
                            NumberBatchesInProgress = int.Parse(e.Value);
                            break;
                        case "numberBatchesCompleted":
                            NumberBatchesCompleted = int.Parse(e.Value);
                            break;
                        case "numberBatchesFailed":
                            NumberBatchesFailed = int.Parse(e.Value);
                            break;
                        case "numberBatchesTotal":
                            NumberBatchesTotal = int.Parse(e.Value);
                            break;
                        case "numberRecordsProcessed":
                            NumberRecordsProcessed = int.Parse(e.Value);
                            break;
                        case "numberRetries":
                            NumberRetries = int.Parse(e.Value);
                            break;
                        case "numberRecordsFailed":
                            NumberRecordsFailed = int.Parse(e.Value);
                            break;
                        case "totalProcessingTime":
                            TotalProcessingTime = int.Parse(e.Value);
                            break;
                        case "apiActiveProcessingTime":
                            ApiActiveProcessingTime = int.Parse(e.Value);
                            break;
                        case "apexProcessingTime":
                            ApexProcessingTime = int.Parse(e.Value);
                            break;
                    }
                }
            }
        }
    }
}