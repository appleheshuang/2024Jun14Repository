using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace Sailfish.RealignmentEngine.Salesforce
{
    [ExcludeFromCodeCoverage]
    public class SalesforceRestAPI : ISalesforceRestAPI
    {
        private string _sessionId;
        private string _instanceUrl;

        public SalesforceRestAPI(SfdcCredentials sfdcCredentials)
        {
            _sessionId = GetSessionId(sfdcCredentials);
            _instanceUrl = sfdcCredentials.InstanceUrl;
        }

        public string AsyncService { get { return "services/async/45.0"; } }

        public string DataService { get { return "services/data/v45.0"; } }

        public string Invoke(String reqUrl, string methodType, string contentType, Byte[] body)
        {
            return InvokeHttpRequest(reqUrl, _sessionId, methodType, contentType, body);
        }

        public string Invoke(String reqUrl, string methodType, string contentType)
        {
            return InvokeHttpRequest(reqUrl, _sessionId, methodType, contentType);
        }

        private string InvokeHttpRequest(String reqUrl, string sessionId, string methodType, string contentType, Byte[] body = null)
        {
            string response;
            try
            {
                WebRequest requestHttp = WebRequest.Create(string.Format("{0}/{1}", _instanceUrl, reqUrl));
                requestHttp.Method = methodType;
                requestHttp.ContentType = contentType;

                if (reqUrl.StartsWith(AsyncService))
                    requestHttp.Headers.Add((string.Format("X-SFDC-Session: {0}", _sessionId)));
                else
                    requestHttp.Headers.Add("Authorization", string.Format("Bearer {0}", _sessionId));

                if (body != null)
                {
                    requestHttp.ContentLength = body.Length;
                    Stream strmHttpContent = requestHttp.GetRequestStream();
                    strmHttpContent.Write(body, 0, body.Length);
                    strmHttpContent.Close();

                }
                using (WebResponse responseHttpRequest = requestHttp.GetResponse())
                {
                    Stream responseStream = responseHttpRequest.GetResponseStream();
                    response = new StreamReader(responseStream ?? throw new InvalidOperationException()).ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    WebResponse wr = ex.Response;
                    string msg = new StreamReader(wr.GetResponseStream() ?? throw new InvalidOperationException()).ReadToEnd().Trim();
                    throw new RealignmentEngineException(msg, ex);
                }
                throw new RealignmentEngineException(string.Format("Exception in {0} method with URL {1}.", methodType, reqUrl), ex);
            }
            return response;
        }

        public void DownloadFile(String reqUrl, string filename)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers.Add(string.Format("X-SFDC-Session: {0}", _sessionId));
                client.DownloadFile(string.Format("{0}/{1}", _instanceUrl, reqUrl), filename);
            }
        }

        public string UpdateSObject(string objectType, string objectId, string json)
        {
            string url = string.Format("{0}/sobjects/{1}/{2}?_HttpMethod=PATCH", DataService, objectType, objectId);
            string contentType = "application/json; charset=UTF-8";
            string method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(json);
            string response = Invoke(url, method, contentType, bytes);
            return response;
        }

        public string RetrieveSObject(string objectType, string objectId)
        {
            string url = string.Format("{0}/sobjects/{1}/{2}", DataService, objectType, objectId);
            string contentType = "application/json; charset=UTF-8";
            string method = "GET";
            string response = Invoke(url, method, contentType);
            return response;
        }

        public string ExexcuteSOQL(string soql)
        {
            string url = string.Format("{0}/query/?q={1}", DataService, soql);
            string contentType = "application/json; charset=UTF-8";
            string method = "GET";
            string response = Invoke(url, method, contentType);
            return response;
        }

        private string GetSessionId(SfdcCredentials sfdcCredentials)
        {
            string authServiceUrl = string.Format("{0}/services/oauth2/token", sfdcCredentials.InstanceUrl);
            string data = string.Format("grant_type=password&client_id={0}&client_secret={1}&username={2}&password={3}",
                HttpUtility.UrlEncode(sfdcCredentials.ClientId),
                HttpUtility.UrlEncode(sfdcCredentials.ClientSecret),
                HttpUtility.UrlEncode(sfdcCredentials.Username),
                HttpUtility.UrlEncode(sfdcCredentials.Password));

            string sessionId;
            try
            {
                WebRequest requestHttp = WebRequest.Create(authServiceUrl);
                requestHttp.Method = "POST";
                requestHttp.ContentType = "application/x-www-form-urlencoded";
                byte[] bytes = Encoding.ASCII.GetBytes(data);
                requestHttp.ContentLength = bytes.Length;
                Stream strmHttpContent = requestHttp.GetRequestStream();
                strmHttpContent.Write(bytes, 0, bytes.Length);
                strmHttpContent.Close();
                requestHttp.Credentials = CredentialCache.DefaultCredentials;

                using (WebResponse responseHttpRequest = requestHttp.GetResponse())
                {
                    Stream responseStream = responseHttpRequest.GetResponseStream();
                    string json = new StreamReader(responseStream ?? throw new InvalidOperationException()).ReadToEnd();
                    sessionId = JObject.Parse(json)["access_token"].ToString();
                }

            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    WebResponse wr = ex.Response;
                    string msg = new StreamReader(wr.GetResponseStream()).ReadToEnd().Trim();
                    Console.WriteLine(msg);
                }
                throw;
            }
            return sessionId;
        }
    }

    [ExcludeFromCodeCoverage]
    public class SfdcCredentials
    {
        public string InstanceUrl { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string Username { get; }
        public string Password { get; }

        public SfdcCredentials(string _instanceUrl, string _clientId, string _clientSecret, string _username, string _password)
        {
            InstanceUrl = _instanceUrl;
            ClientId = _clientId;
            ClientSecret = _clientSecret;
            Username = _username;
            Password = _password;
        }
    }
}