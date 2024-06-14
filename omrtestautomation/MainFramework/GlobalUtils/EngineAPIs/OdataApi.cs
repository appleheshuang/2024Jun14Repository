using System;
using System.Net;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MainFramework.GlobalUtils
{
    public struct connectivity_node
    {
        public string OCE { get; }
        public string OMR { get; }
        public connectivity_node(string omr = "unkown", string oce = "unkown") { OMR = omr; OCE = oce; }
    }
    public class OdataApi : OptimizerApi
    {
        private string tenant_endpt;
        public OdataApi(TenantConfig tenant_info, eToken token)
            : base(tenant_info, token)
        {
            tenant_endpt = tenant_info.TenantId;
        }

        public string JobStatus()
        {
            return "OK";
        }
        public string ScenarioStatus(string scenarioid)
        {
            string oreq = "/OMScenario?$select = ScenarioStatus &$filter = Id eq '" + scenarioid + "'";
            return GetSingleValue("ScenarioStatus", oreq, "STATIC", scenarioid);
        }
        public string GetSingleValue(string field, string req, string _schema, string scenarioid = null)
        {
            var chkbody = GetOdata(req, _schema, scenarioid);
            try
            {
                JObject joChk = JObject.Parse(chkbody);
                if (joChk.ContainsKey("value"))
                    return joChk["value"].ToString() == "[]" ? "" : joChk["value"][0][field].ToString();
                else if (joChk.ContainsKey(field))
                    return joChk[field].ToString();
                else return joChk.ToString();
            }
            catch
            {
                Console.WriteLine("Unexpected odata response\n Request=" + tenant_endpt + req + ", Field=" + field + "\n\tResponse=" + chkbody.ToString());
                return field + " not found in " + chkbody.ToString();
            }
        }
        public string GetOdata(string req, string schema, string scenarioid = null)
        {
            List<Tuple<string, string>> headers = SetHeaders(schema, scenarioid);
            Tuple<IRestResponse, string> odataResponse = Request_RestSharp("odataRoot", "GET", api_param: tenant_endpt + req, add_headers: headers);
            if (odataResponse.Item1.StatusCode == HttpStatusCode.GatewayTimeout) // retry work-around for OMR-10281
            {
                Console.WriteLine("Warning: Odata GatewayTimeout for: " + tenant_endpt + req + " - may be related to OMR-10281");
                odataResponse = Request_RestSharp("odataRoot", "GET", api_param: tenant_endpt + req, add_headers: headers);
            }
            if (odataResponse.Item1.StatusCode == HttpStatusCode.OK)
                return odataResponse.Item2.ToString();
            else
                return odataResponse.Item1.StatusCode.ToString() + " " + odataResponse.Item2.ToString();
        }
        /// <summary>
        /// Post to odata - used by OData POST with $query, MergeAPI
        /// </summary>
        /// <param name="req"></param>
        /// <param name="_schema"></param>
        /// <param name="scenarioid"></param>
        /// <returns></returns>
        public string PostOdata(string req, object requestBody, string schema = null, string scenarioid = null)
        {
            List<Tuple<string, string>> headers = SetHeaders(schema, scenarioid);
            Tuple<IRestResponse, string> odataResponse = Request_RestSharp("odataRoot", "POST", request_body: requestBody, api_param: tenant_endpt + req, add_headers: headers);
            if (odataResponse.Item1.StatusCode == HttpStatusCode.OK)
                return odataResponse.Item2.ToString();
            else
                return odataResponse.Item1.StatusCode.ToString() + " " + odataResponse.Item2.ToString();
        }

        /// <summary>
        /// SetHeaders - currently just the schema id
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        List<Tuple<string, string>> SetHeaders(string schema, string id)
        {
            List<Tuple<string, string>> thisHeader = new List<Tuple<string, string>>();
            if (schema != null && schema.EndsWith("_OUTPUT"))
            {
                thisHeader.Add(new Tuple<string, string>("OMSchemaId", id.ToUpper()));
            }
            return thisHeader;
        }
        public HttpStatusCode PatchSingleValue(string table, string id, string attr, string value)
        {
            JObject body = new JObject();
            switch (table)
            {
                case "OMAccount":
                    body.Add("Id", id);
                    break;
                case "OMProduct":
                    body.Add("SOMProductId", id);
                    break;
            }
            body.Add(attr, value);
            Tuple<IRestResponse, string> odataResponse = Request_RestSharp("odataRoot", "PATCH", request_body: body, api_param: tenant_endpt + "/" + table);
            return odataResponse.Item1.StatusCode;
        }
        public int QueuedJobCount()
        {
            string qc = GetSingleValue("QueueCount", "/SystemStatus", "STATIC");
            try
            {
                return Int16.Parse(qc);
            }
            catch
            {
                Console.WriteLine("Unexpected QueueCount value [" + qc + "]\n");
                return -1;
            }
        }
        public bool EngineIsRunning()
        {
            try { return GetSingleValue("EngineStatus", "/SystemStatus", "STATIC") == "Running"; }
            catch { return true; } // If we can't get the status, assume it's running
        }
        public string SystemStatusNode(string node)
        {
            return GetSingleValue(node, "/SystemStatus", "STATIC");
        }
        public connectivity_node GetConnectivityStatus()
        {
            var chkbody = GetOdata("/SystemStatus", "STATIC");
            try
            {
                JObject joChk = JObject.Parse(chkbody);
                return JsonConvert.DeserializeObject<connectivity_node>(joChk["Connectivity"].ToString());
                //return new connectivity_node(joChk["Connectivity"]["OMR"].ToString(), joChk["Connectivity"]["OCE"].ToString());
            }
            catch
            {
                Console.WriteLine("/SystemStatus Connectivity check failed\n Request=" + tenant_endpt + "/SystemStatus\n\tResponse=" + chkbody.ToString());
                return new connectivity_node();
            }
        }

        public JArray AccountMerge(object mergeBody)
        {
            //Run MergeAPI
            var responseBody = PostOdata("/merge", mergeBody);
            return ParseResponse(responseBody);
        }

        public JArray ParseResponse(string response)
        {
            JObject joresp;
            try
            {
                joresp = JObject.Parse(response);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse response Json: " + response + "\nRequest: " + _request, e);
            }
            return JArray.Parse(joresp["value"].ToString());
        }
    }

}