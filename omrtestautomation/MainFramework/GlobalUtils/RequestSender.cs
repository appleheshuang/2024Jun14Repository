using System;
using System.IO;
using System.Net;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;


namespace MainFramework.GlobalUtils
{
    public class RequestSender
    {

        public Tuple<IRestResponse, String> GET_RestSharp(string strRequest, Hashtable ht, string strMethod, string strBody){
            var client = new RestClient(strRequest);
            var request = new RestRequest(); 
            switch(strMethod){
                case "GET":
                    request.Method = Method.GET;
                    break;
                case "POST":
                    request.Method = Method.POST;
                    break;
                case "PATCH":
                    request.Method = Method.PATCH;
                    break;
                case "DELETE":
                    request.Method = Method.DELETE;
                    break;
                default:
                    request.Method = Method.GET;
                    break;
            }            
            foreach(DictionaryEntry pair in ht){
                var strHeader = pair.Key;
                var strHdrValue = pair.Value;
                request.AddHeader(strHeader.ToString(), strHdrValue.ToString());                
            }
            request.AddHeader("Accept", "application/json");

            if (strBody != "NONE")
            {
                request.AddJsonBody(strBody.ToString());
            }

            IRestResponse response = client.Execute(request);
            string strResponse;
            try
            {
                if (response.Content.GetType() == "string".GetType()) strResponse = response.Content.ToString();
                else
                {
                    var varResponse = JsonConvert.DeserializeObject(response.Content);
                    strResponse = (varResponse ?? "").ToString();
                }
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine("Error while parsing Get_RestSharp response...");
                Console.WriteLine("Get_RestSharp request: Uri=" + strRequest + " | Method=" + strMethod + " | Body=" + strBody);
                Console.WriteLine("Get_RestSharp response: Status=" + response.StatusCode + " | Desciption=" + response.StatusDescription + " | Error=" + response.ErrorMessage);
                Console.WriteLine("Get_RestSharp response: Content=" + response.Content);
                throw e;
            }

            return new Tuple<IRestResponse, String>(response, strResponse);
        }  

        public Tuple<WebResponse, String> RETURN_WebResponse(string strRequest, string strMethod, Hashtable ht, string strBody, string strFormData){
            HttpWebRequest _webRequest = (HttpWebRequest)WebRequest.Create(strRequest);
            _webRequest.Method = strMethod;
            foreach(DictionaryEntry pair in ht){
                var strHeader = pair.Key;
                var strHdrValue = pair.Value;
                _webRequest.Headers[strHeader.ToString()] = strHdrValue.ToString();
            }
            if (strBody != "NONE"){
                string stringData = strBody;
                var data = Encoding.ASCII.GetBytes(stringData);
                using (var stream = _webRequest.GetRequestStream()){
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }
            }

            if (strFormData != "NONE")
            {
                byte[] dataByte = Encoding.ASCII.GetBytes(strFormData);
                _webRequest.ContentLength = dataByte.Length;
                Stream strmHttpContent = _webRequest.GetRequestStream();
                strmHttpContent.Write(dataByte, 0, dataByte.Length);
                strmHttpContent.Close();
                _webRequest.Credentials = CredentialCache.DefaultCredentials;
            }

            try
            {
                HttpWebResponse res = (HttpWebResponse)_webRequest.GetResponse();
                using (Stream dataStream = res.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    dataStream.Close();
                    reader.Close();
                    return new Tuple<WebResponse, String>(res, responseFromServer);
                }
            }
            catch (WebException e)
            {
                WebResponse res = e.Response;
                string responseFromServer = "Error Sending the request";
                return new Tuple<WebResponse, String>(res, responseFromServer);
            }                      
            
        }

        public string GET_token(string xapikey, string strBody, string strEndpoint, string username, string password, string tokenType){
            //encode credentials
            string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("utf-8").GetBytes(username + ":" + password));
            //arrange headers
            Hashtable headers = new Hashtable();
                headers.Add("content-type", "application/json");
                headers.Add("x-api-key", xapikey);
                headers.Add("Authorization", "Basic " + encoded);
            //send request and parse the response
            Tuple<WebResponse, String> responsTuple = this.RETURN_WebResponse(strEndpoint, "POST", headers, strBody, "NONE");
            WebResponse response = responsTuple.Item1;
            var body = responsTuple.Item2;
            //send the token
            JObject bodyP = JObject.Parse(body);
            var token = bodyP[tokenType];
            return token.ToString();     
        }

		public JObject GET_refresh_token(string xapikey, string strEndpoint, string username, string password)
		{
			//encode credentials
			string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("utf-8").GetBytes(username + ":" + password));
			//arrange headers
			Hashtable headers = new Hashtable();
			headers.Add("content-type", "application/json");
			headers.Add("x-api-key", xapikey);
			headers.Add("Authorization", "Basic " + encoded);
			//send request and parse the response
			Tuple<WebResponse, String> responsTuple = this.RETURN_WebResponse(strEndpoint, "POST", headers, "NONE", "NONE");
            try
            {
                return JObject.Parse(responsTuple.Item2);
            }
            catch (Exception e)
            {
                string req_info = "Request: " + strEndpoint + ",\n credentials = " + username + " | " + password;
                throw new Exception("GetToken error parsing response.\n"+ req_info+ "\nResponse body = " + responsTuple.Item2, e);
            }
		}

		public Tuple<HttpWebResponse, T> RETURN_HTTPWebResponse<T>(string strRequest, string strMethod, Hashtable ht, string strBody)
        {
            WebRequest _webRequest = WebRequest.Create(strRequest);
            _webRequest.Method = strMethod;
            foreach (DictionaryEntry pair in ht)
            {
                var strHeader = pair.Key;
                var strHdrValue = pair.Value;
                _webRequest.Headers[strHeader.ToString()] = strHdrValue.ToString();
            }

            if (strBody != "NONE")
            {
                string stringData = strBody;
                var data = Encoding.ASCII.GetBytes(stringData);
                using (var stream = _webRequest.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }
            }

            HttpWebResponse res;
            using (var ms = new MemoryStream())
            {

                try
                {
                    res = _webRequest.GetResponse() as HttpWebResponse;
                    res.GetResponseStream().CopyTo(ms);
                }

                catch (WebException e)
                {
                    WebResponse response = e.Response;
                    res = (HttpWebResponse)response;
                    // res.GetResponseStream().CopyTo(ms);
                    return new Tuple<HttpWebResponse, T>(res, default(T));

                }
                try
                {
                    var output = JsonConvert.DeserializeObject<T>(System.Text.Encoding.UTF8.GetString(ms.ToArray()));
                    return new Tuple<HttpWebResponse, T>(res, output);
                }
                catch
                {
                    return new Tuple<HttpWebResponse, T>(res, default(T));
                }


            }
        }

        public class TokenApiResponseBody
        {
            public string token { get; set; }

            public string refresh { get; set; }
        }


        public Tuple<HttpWebResponse, T> FetchResponse<T>(string strEndpoint, Hashtable headers, string body, string method)
        {
            return this.RETURN_HTTPWebResponse<T>(strEndpoint, method, headers, body);
        }

        public static string CognitoMasterTokenCredentials()
        {
            GlobalConfigFixture config = new GlobalConfigFixture();
            var masterkey = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(config.GConfig.CognitoMasterTokenUsername + ":" + config.GConfig.CognitoMasterTokenPassword));
            // var masterkey = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("master-tenantcreator" + ":" + "8xsTbcsCvHwa2bwvGQ5D1pYytwmURm"));
            return masterkey;
        }


    }
}