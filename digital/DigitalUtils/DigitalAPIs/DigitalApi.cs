using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using RestSharp;
using System.Net;
using MainFramework.Constructors;

namespace MainFramework.GlobalUtils
{
    public class DigitalApi
    {
        public string _scenarioString = String.Empty;
        public string _scenarioData = String.Empty;
        public string DataUrl;
        public string SFNameSpace;
        public string token;
        DigitalDataCollector ddc;

        public DigitalApi(TenantConfig t)
        {
            var serialized = JsonConvert.SerializeObject(t);
            var dt = JsonConvert.DeserializeObject<DigitalTenantConfig>(serialized);

            ddc = new DigitalDataCollector(dt);
            token = ddc.GET_MCMToken();
            DataUrl = "https://" + ddc._clientSubdomain + ".rest.marketingcloudapis.com/hub/v1/dataevents/key:";
        }

        private string buildPostBody(Object obj)
        {
            string _scenarioString = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return _scenarioString;
        }

        public JObject GetResponseBody(string strRequest, string requesttype, string strBody, HttpStatusCode expCode = HttpStatusCode.OK)
        {
            RequestSender _sendObj = new RequestSender();
            Hashtable headers2 = new Hashtable();
            headers2.Add("content-type", "application/json");
            headers2.Add("Authorization", "Bearer " + token);
            Tuple<IRestResponse, String> apiResponse = _sendObj.GET_RestSharp(strRequest, headers2, requesttype, strBody);

            if (apiResponse.Item1.IsSuccessful)
            {
                var response = apiResponse.Item1;
                if (expCode == response.StatusCode)
                {
                    var responseBody = apiResponse.Item2;
                    if (responseBody != "")
                    {
                        return JObject.Parse(responseBody);

                    }
                    else return null;
                }
            }
            else
            {
                var failedResponse = new Exception("Error retrieving response for " + strRequest + "\nError message: " + apiResponse.Item2 + "\nError exception: " + apiResponse.Item1.ErrorException);
                throw failedResponse;
            }
            return null;
        }

        public void Post_Object(string tablename, Object obj)
        {
            try
            {
                String teststr = obj.ToString();
                JObject responseBody = GetResponseBody(DataUrl+ tablename+"/rowset", "POST", obj.ToString(), HttpStatusCode.Created);
            }
            catch (Exception e)
            {
                throw new Exception("Failed with request: " + buildPostBody(obj) + "\n With exception during POST: " + e.Message);
            }
        }
    }
}
