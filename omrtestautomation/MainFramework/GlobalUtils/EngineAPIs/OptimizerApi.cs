using System;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections;
using System.Collections.Generic;

namespace MainFramework.GlobalUtils
{
    public struct eToken
    {
        public string token { get; set; }
        public string refresh { get; set; }
        public string apikey { get; set; }
        public string versionendpoint { get; set; }
        public DateTime token_set_time { get; set; }
        public eToken(string t, string r, string k=null, string v="latest") 
        { token = t; refresh = r; apikey = k; versionendpoint = v; token_set_time = DateTime.UtcNow; }
    }

    public class OptimizerApi
    {
        public TenantConfig t;

        public eToken engine_token;
        public string engine_version; //like 11-1
        public string version_header { //like 11.1.0
            get { return engine_version.Replace('-','.'); }
        }
        public EnvConfig envConfig;

        public RequestSender _sender;
        public static globalCodes _endpoint = new globalCodes("EndPoints.json");
        public static globalCodes _status_code = new globalCodes("StatusCodes.json");

        public const int poll_interval = 10;
        public const int default_max_dur = 600;
        public List<string> done_statuses;
        public string _request { get; set; }
        public string _response { get; set; }
        public string _auth { get; set; }

        private List<string> auth_headers = new List<string> { "x-api-key", "Authorization", "refreshtoken" };
        private List<string> std_headers = new List<string> { "Content-Type" };
        private const int token_lifespan_min = 59;

        public OptimizerApi(TenantConfig tenant_info, eToken got_token)
	    {
            t = tenant_info;
            // Get a token for the tenant
            _sender = new RequestSender();
            SetToken(got_token,t.TenantUser,t.TenantPassword);
            //SetVersion();
            done_statuses = new List<string> { };
        }

        public OptimizerApi(TenantConfig tenant_info)
        {
            t = tenant_info;
            _sender = new RequestSender();
            engine_token = new eToken("", "");
            if (t.TenantUser != null && t.TenantUser != "" && t.TenantPassword != null && t.TenantPassword != "") {
                GetToken();
            }
            SetVersion();
        }

        public string EndPoint(string api_name, bool isDigital = false)
        {
            if (isDigital) 
                return AppEndpoint(api_name, "digital");
            else
                return t.endpoint + engine_version + _endpoint[api_name];
        }
        public string AppEndpoint(string api_name, string app = "")
        {
                return t.endpoint + app + '/' + engine_version + _endpoint[api_name];
        }

        public eToken GetToken(string username=null, string pswd = null, string apikey = null)
        {
            if (engine_token.token.Length == 0 || engine_token.token == null)
            {
                SetVersion();
                JObject token_obj = _sender.GET_refresh_token(apikey ?? t.sfApiKey, EndPoint("token"), username ?? t.TenantUser, pswd ?? t.TenantPassword);
                engine_token.token = token_obj["token"].ToString();
                engine_token.refresh = token_obj["refresh"].ToString();
                engine_token.apikey = apikey ?? t.sfApiKey;
            }
            return engine_token;
        }
        // Having a token from sf /lexitoken request, set the token to use for engine api
        public void SetToken(string _token, string _refresh, string _apikey=null, string _ver="latest")
        {
            engine_token.token = _token;
            engine_token.refresh = _refresh;
            engine_token.apikey = _apikey;
            engine_token.versionendpoint = _ver;
            engine_token.token_set_time = DateTime.UtcNow;
        }
        public void SetToken(eToken _token, string username = null, string pswd = null)
        {
            SetVersion();
            if (_token.token is null || _token.refresh is null)
            {
                if ((username is null && (t.TenantUser is null || t.TenantUser == "")) || (pswd is null && (t.TenantPassword is null || t.TenantPassword == "")))
                // Get the token from salesforce lexitoken if the cognito credentials are not known
                    try
                    {
                        SalesforceApi sftester = new SalesforceApi(t);
                        engine_token = sftester.GetEngineToken();
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to obtain Salesforce token for " + t.orgURL + " | " + t.orgUserName + " | " + t.orgPassword, e);
                    }
                else
                {
                    JObject token_obj = _sender.GET_refresh_token(t.sfApiKey, EndPoint("token"), username ?? t.TenantUser, pswd ?? t.TenantPassword);
                    engine_token.token = token_obj["token"].ToString();
                    engine_token.refresh = token_obj["refresh"].ToString();
                    engine_token.token_set_time = DateTime.UtcNow;
                }
            }
            else
            {
                engine_token = _token;
            }
            SetVersion();
        }

        public string WaitForStatus(Func<string> JobStatusMethod, int intWait = poll_interval, int max_dur = default_max_dur)
        {
            int elapsed_time = 0;
            string jobStatus = JobStatusMethod();
            while (!done_statuses.Contains(jobStatus) && elapsed_time <= max_dur)
            {
                System.Threading.Thread.Sleep(intWait * 1000);
                elapsed_time += intWait;
                jobStatus = JobStatusMethod();
            }
            return jobStatus;
        }

        public void SetVersion()
        {
            if (t.targetEnv == "master" || t.targetEnv == "demo" ) // target latest version
            {
                engine_version = "latest";
            }
            else if (t.targetVersion == null || t.targetVersion == "")
            {
                engine_version = engine_token.versionendpoint ?? "latest";
            }
            else
            {
                engine_version = t.targetVersion;
            }
        }

        void SetAPIInfo(string endpoint, Hashtable headers, string action, string body, string response = null)
        {
            _request = "\t" + action + ": " + endpoint + "\n\tBody: " + body;
            _auth = "";
            string extra_headers = "";
            foreach (string header_key in headers.Keys)
            {
                if (auth_headers.Contains(header_key))
                    _auth += "\n\t\t" + header_key + ": " + headers[header_key];
                else if (!std_headers.Contains(header_key))
                    extra_headers += "\n\t\t" + header_key + ": " + headers[header_key];
            }
            _request += extra_headers == "" ? "" : ("\n\tHeaders: " + extra_headers);
            if (!(response is null || response == ""))
                _response = "\n\tResponse: " + response;
        }
        void SetAPIResponse(string response)
        {
            _response = "\nResponse: " + response;
        }
        Hashtable ResetAuth(Hashtable auth)
        {
            SetToken(new eToken(null, null));
            auth.Remove("Authorization");
            auth.Remove("refreshtoken");
            auth.Add("Authorization", "Bearer " + engine_token.token);
            auth.Add("refreshtoken", engine_token.refresh);
            return auth;
        }

        public Tuple<IRestResponse, String> Request_RestSharp(string api, string action, object request_body=null, string api_param="", List<Tuple<string, string>> add_headers=null, bool isDigital=false, string query_tag=null)
        {
            string postUrl = EndPoint(api, isDigital)+api_param;
            Hashtable headers = new Hashtable();
            foreach (Tuple<string, string> this_header in add_headers ?? new List<Tuple<string, string>>())
            {
                headers.Add(this_header.Item1, this_header.Item2);
            }
            headers = AddStandardHeaders(headers, query_tag);
            var body = request_body==null ? "NONE" : JsonConvert.SerializeObject(request_body);
            Tuple<IRestResponse, String> response = _sender.GET_RestSharp(postUrl, headers, action, body);
            SetAPIInfo(postUrl, headers, action, body, response.Item2);
            if (response.Item1.StatusCode == HttpStatusCode.Unauthorized)
            {
                int token_age = token_age_min();
                if (token_age >= token_lifespan_min)
                {
                    headers = ResetAuth(headers);
                    response = _sender.GET_RestSharp(postUrl, headers, action, body);
                    SetAPIInfo(postUrl, headers, action, body, response.Item2);
                }
                else
                {
                    throw new UnauthorizedTokenException(postUrl, token_age, engine_token);
                }
            }
            return response;
        }
        public Tuple<IRestResponse, String> Request_RestSharp_NoAuth(string api, string action, object request_body = null, string api_param = "", List<Tuple<string, string>> add_headers = null, bool isDigital = false)
        {
            string postUrl = EndPoint(api, isDigital) + api_param;
            if (api.Contains("segmentation")) postUrl = AppEndpoint(api, "segmentation");

            //new request headers
            Hashtable headers = new Hashtable();
            foreach (Tuple<string, string> h in add_headers ?? new List<Tuple<string, string>>())
                headers.Add(h.Item1, h.Item2);
            if (!headers.ContainsKey("Content-Type")) headers.Add("Content-Type", "application/json");

            var body = request_body == null ? "NONE" : JsonConvert.SerializeObject(request_body);
            Tuple<IRestResponse, String> response = _sender.GET_RestSharp(postUrl, headers, action, body);
            SetAPIInfo(postUrl, headers, action, body, response.Item2);
            return response;
        }
        public string GetValuefromResponseBody(string field, string chkbody)
        {
            try
            {
                JObject joChk = JObject.Parse(chkbody);
                if (joChk.ContainsKey(field))
                    return joChk[field].ToString();
                else return joChk.ToString();
            }
            catch
            {
                JArray joRes = JArray.Parse(chkbody);
                if (joRes.Count == 0)
                    return "";
                else
                {
                    var first = joRes.First;
                    JObject joChk = JObject.Parse(first.ToString());
                    if (joChk.ContainsKey(field))
                        return joChk[field].ToString();
                    else return joChk.ToString();
                }
            }
        }
        public string GetValuefromResponseBody(string field, string chkbody, string lookup_clm, string lookup_value)
        {
            try
            {
                JArray joRes = JArray.Parse(chkbody);
                if (joRes.Count == 0)
                    return "";
                else
                {
                    foreach (JToken x in joRes)
                    {
                        JObject joChk = JObject.Parse(x.ToString());
                        if (joChk[lookup_clm].ToString() == lookup_value)
                            return joChk[field].ToString();
                    }
                    Console.WriteLine("\nWarning: [" + lookup_clm + "=" + lookup_value + "] not found in " + chkbody);
                    Console.WriteLine("\nRequest:\n"+_request);
                    return chkbody;
                }
            }
            catch
            {
                Console.WriteLine("Exception looking for elements [" + field + "],[" + lookup_clm + "] in " + chkbody);
                return chkbody;
            }
        }
        public List<string> GetValuesfromResponseBody(string field, string chkbody)
        {
            List<string> values = new List<string>();
            JArray joRes = JArray.Parse(chkbody);
            if (joRes.Count == 0)
                return values;
            else
            {
                foreach (JToken element in joRes)
                {
                    JObject joString = JObject.Parse(element.ToString());
                    if (joString.ContainsKey(field))
                    {
                        values.Add(joString[field].ToString());
                    } else
                    {
                        continue;
                    }
                }
            }
            return values;
        }    
        int token_age_min()
        {
            return (int)DateTime.UtcNow.Subtract(engine_token.token_set_time).TotalMinutes;
        }
        public string Request_RestSharpValue(string field, string api, string action, object request_body = null, string api_param = "", List<Tuple<string, string>> extra_headers = null)
        {
            Tuple<IRestResponse, String> response = Request_RestSharp(api, action, request_body, api_param, extra_headers);
            try
            {
                return GetValuefromResponseBody(field, response.Item2);
            }
            catch
            {
                return response.Item1.StatusCode.ToString() + ":" + response.Item1.StatusDescription + ", Response body=" + response.Item2;
            }
        }

        Hashtable AddStandardHeaders(Hashtable header_list, string app_value)
        {
            if (!header_list.ContainsKey("Content-Type")) header_list.Add("Content-Type", "application/json");
            if (!header_list.ContainsKey("x-api-key")) header_list.Add("x-api-key", t.sfApiKey);
            if (!header_list.ContainsKey("Authorization")) header_list.Add("Authorization", "Bearer " + engine_token.token);
            if (!header_list.ContainsKey("refreshtoken")) header_list.Add("refreshtoken", engine_token.refresh);
            if (app_value != null && app_value != "" && !header_list.ContainsKey("app"))
                header_list.Add("app", app_value);
            return header_list;
        }
        public Tuple<HttpWebResponse, T> Request<T>(string api, string action, object request_body = null, string api_param = "", List<Tuple<string, string>> add_headers = null, string query_tag=null)
        {
            string postUrl = EndPoint(api) + api_param;
            Hashtable headers = new Hashtable();
            foreach(Tuple<string, string> this_header in add_headers ?? new List<Tuple<string, string>>())
            {
                headers.Add(this_header.Item1, this_header.Item2);
            }
            headers = AddStandardHeaders(headers, query_tag);
            var body = request_body == null ? "NONE" : JsonConvert.SerializeObject(request_body);
            Tuple<HttpWebResponse, T> response = _sender.FetchResponse<T>(postUrl, headers, body, action);
            string resp_info = response.Item1.StatusCode == HttpStatusCode.OK ? response.Item2.ToString() : response.Item1.StatusCode.ToString() + (response.Item2 != null ? "; Msg="+ response.Item2.ToString() : "");
            SetAPIInfo(postUrl, headers, action, body, resp_info);
            return response;
        }
        public HttpWebResponse RequestStatus(string api, string action, object request_body = null, string api_param = "", List<Tuple<string, string>> extra_headers = null)
        {
            Tuple<HttpWebResponse, string> response = Request<string>(api, action, request_body, api_param, extra_headers);
            return response.Item1;
        }


    }
    public class UnauthorizedTokenException : Exception
    {
        eToken t;
        public UnauthorizedTokenException(string req_info, int token_age, eToken token)
        {
            t = token;
            throw new Exception(req_info + token_info(token_age));
        }

        public UnauthorizedTokenException(string req_info, int token_age, eToken token, Exception inner)
        {
            t = token;
            throw new Exception(req_info + token_info(token_age), inner);
        }
        string token_info(int token_age_min)
        {
            string token_time = "\n\tToken time UTC="+t.token_set_time.ToString("yyMMdd-hh:mm:ss") + "\n\tCurrent time UTC="+ DateTime.UtcNow.ToString("yyMMdd-hh:mm:ss");
            string token_info = "\n\tToken=" + t.token + "\n\tRefresh=" + t.refresh;
            string token_age = "\n\tToken age (min)="+token_age_min.ToString();
            return token_age + token_time + token_info;
        }

    }

}