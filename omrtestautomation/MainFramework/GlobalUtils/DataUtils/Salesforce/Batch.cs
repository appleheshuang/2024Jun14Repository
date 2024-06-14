using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Sailfish.RealignmentEngine.Salesforce
{
    public class Batch
    {
        private ISalesforceRestAPI _restAPI;
        public String Id { get; set; }
        public String JobId { get; set; }
        public String State { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime SystemModStamp { get; set; }
        public int NumberRecordsProcessed { get; set; }
        public int NumberRecordsFailed { get; set; }
        public int TotalProcessingTime { get; set; }
        public int ApiActiveProcessingTime { get; set; }
        public int ApexProcessingTime { get; set; }

        public Batch(string jobId, string query, ISalesforceRestAPI restApi)
        {
            _restAPI = restApi;
            string url = string.Format("{0}/job/{1}/batch", _restAPI.AsyncService, jobId);
            string contentType = "text/csv; charset=UTF-8";
            string method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(query);
            string response = _restAPI.Invoke(url, method, contentType, bytes);
            ParseBatchInfo(response);
        }

        public void Refresh()
        {
            string url = string.Format("{0}/job/{1}/batch/{2}", _restAPI.AsyncService, JobId, Id);
            string contentType = "text/csv; charset=UTF-8";
            string method = "GET";
            string response = _restAPI.Invoke(url, method, contentType);
            ParseBatchInfo(response);
        }

        public List<string> GetResultList()
        {
            string url = string.Format("{0}/job/{1}/batch/{2}/result", _restAPI.AsyncService, JobId, Id);
            string contentType = "text/csv; charset=UTF-8";
            string method = "GET";
            string response = _restAPI.Invoke(url, method, contentType);

            XDocument doc = XDocument.Parse(response);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("ns", doc.Root?.GetDefaultNamespace().ToString());

            return doc.Root.XPathSelectElements("/ns:result-list/ns:result", nsmgr).Select(x => x.Value).ToList();
        }

        public void DownloadResult(string resultId, string filename)
        {
            string url = string.Format("{0}/job/{1}/batch/{2}/result/{3}", _restAPI.AsyncService, JobId, Id, resultId);
            _restAPI.DownloadFile(url, filename);
        }

        private void ParseBatchInfo(string resultXml)
        {
            XDocument doc = XDocument.Parse(resultXml);
            XElement batchInfoElement = doc.Root;
            List<XElement> jobInfoChildElements = batchInfoElement?.Elements().ToList();

            Debug.Assert(jobInfoChildElements != null, nameof(jobInfoChildElements) + " != null");
            foreach (XElement e in jobInfoChildElements)
            {
                switch (e.Name.LocalName)
                {
                    case "id":
                        Id = e.Value;
                        break;
                    case "jobId":
                        JobId = e.Value;
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
                    case "numberRecordsProcessed":
                        NumberRecordsProcessed = int.Parse(e.Value);
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