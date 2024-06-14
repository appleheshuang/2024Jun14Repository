using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Data;
using Xunit;
using Xunit.Abstractions;
using Snowflake.Data.Client;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MainFramework.GlobalUtils
{
    public class HTTPClient_AsyncRequest
    {
        private HttpClient _client;

        public HTTPClient_AsyncRequest(){
            _client = new HttpClient();
        }

        public void SET_BaseAddress(string strBaseAddress){
            _client.BaseAddress = new Uri(strBaseAddress);
        }

        public void ADD_Accept(string strHeader){
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(strHeader));
        }

        public void ADD_Headers(string strKey, string strValue){            
            _client.DefaultRequestHeaders.Add(strKey.ToString(), strValue.ToString());
        }

        public void ADD_Authorization(string strAuth, string strToken){
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(strAuth, strToken);
        }

        public async Task<Tuple<HttpResponseMessage, T>> GET_AsyncRequest<T>(string strRequest){
            HttpResponseMessage response = _client.GetAsync(strRequest).Result;
            var responseContent = await response.Content.ReadAsStringAsync();
            var body = JsonConvert.DeserializeObject<T>(responseContent);            
            return new Tuple<HttpResponseMessage, T>(response, body);
        }

        public async Task<string> GET_AsyncRequest_ResponseOnly(string strRequest){
            HttpResponseMessage response = _client.GetAsync(strRequest).Result;
            var responseContent = await response.Content.ReadAsStringAsync();          
            return responseContent;
        }

    }
}