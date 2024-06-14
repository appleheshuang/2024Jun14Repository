using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Collections;
using Newtonsoft.Json;
using Xunit.Abstractions;
using OMREngineTest.DataUtils.Salesforce;
using Sailfish.RealignmentEngine.Tests;
using System.Threading;
using MainFramework.Constructors;

namespace MainFramework.GlobalUtils
{
    public class DigitalDataCollector
    {
        //public string _baseURL;
        public string _BROKERURL_API_URL;
        public string _enhancedClientId, _enhancedClientSecret, _clientSubdomain;
        private RequestSender _send;
        private string _MCMtoken;
        private string _BusinessUnitConfigurationURL;
        public const string not_found = "Not found";
        public const string index_out_of_range = "Index was out of range";
        private string _BUID;
        private string _CONFIG_API_TOKEN;

        public DigitalDataCollector(DigitalTenantConfig dconfig)
        {
            _BROKERURL_API_URL = dconfig.BROKERURL_API_URL;
            _BUID = dconfig.BUID;
            _CONFIG_API_TOKEN = dconfig.CONFIG_API_TOKEN;
            _send = new RequestSender();
            //_sfHelp = new SnowFlakeHelper();
            _BusinessUnitConfigurationURL = _BROKERURL_API_URL + "/api/1/BusinessUnitConfiguration/" + _BUID;
            SET_BusinessUnitConfiguration();
            SET_MCMToken();
        }

        public void SET_BusinessUnitConfiguration()
        {
            var strRequest = _BusinessUnitConfigurationURL;
            Hashtable headers = new Hashtable();
            headers.Add("content-type", "application/json");
            headers.Add("accept", "application/json");
            headers.Add("X-Business-Unit", _BUID);
            headers.Add("Authorization", _CONFIG_API_TOKEN);
           
            Tuple<WebResponse, String> responsTuple = _send.RETURN_WebResponse(strRequest, "GET", headers, "NONE", "NONE");
            var item1 = responsTuple.Item1;
            var body = responsTuple.Item2;
            JObject jBody = JObject.Parse(body);
            JObject value = (JObject)jBody["value"];
            JObject configuration = (JObject)value["configuration"];
            _enhancedClientId = (string)configuration["enhancedClientId"];
            _enhancedClientSecret = (string)configuration["enhancedClientSecret"];
            _clientSubdomain = (string)configuration["clientSubdomain"];
        }

        public void SET_MCMToken()
        {
            var strRequest = "https://"+ _clientSubdomain + ".auth.marketingcloudapis.com/v2/token";
             //Arrange
            Hashtable headers = new Hashtable();
                headers.Add("content-type", "application/x-www-form-urlencoded");
            var strFormData = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&account_id={2}",
                HttpUtility.UrlEncode(_enhancedClientId),
                HttpUtility.UrlEncode(_enhancedClientSecret),
                HttpUtility.UrlEncode(_BUID));
            //Act
            Tuple<WebResponse, String> responsTuple = _send.RETURN_WebResponse(strRequest, "POST", headers, "NONE", strFormData);
            //Apply
            var body = responsTuple.Item2;
            JObject jBody = JObject.Parse(body);
            _MCMtoken = jBody["access_token"].ToString();
        }

        public string GET_MCMToken()
        {
            return _MCMtoken;
        }
    }
}
