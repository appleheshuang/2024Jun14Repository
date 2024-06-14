using Newtonsoft.Json;
using System;
using System.Collections;
using RestSharp;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MainFramework.GlobalUtils
{
    public class SnowflakeApi: OptimizerApi
    {
        
        const int snowTimeoutDefault = 60;
        string snowToken, snowUrl;
        SnowRequestBody reqBody;
        SnowResponse result;

        public SnowflakeApi(TenantConfig t, eToken e) : base(t, e)
        {
            // Get a snowflake token from the engine
            Tuple<IRestResponse, String> token_response = Request_RestSharp("snowtoken", "POST");
            if (token_response.Item1.StatusCode == HttpStatusCode.OK)
            {
                // Prepare the request body per the values returned
                string responseBody = token_response.Item2;
                reqBody = new SnowRequestBody();
                snowToken = GetValuefromResponseBody("token", responseBody);
                snowUrl = GetValuefromResponseBody("url", responseBody);
                reqBody.database = GetValuefromResponseBody("database", responseBody);
                reqBody.role = GetValuefromResponseBody("role", responseBody);
                reqBody.warehouse = GetValuefromResponseBody("warehouse", responseBody);
                reqBody.timeout = snowTimeoutDefault;
                reqBody.parameters = JObject.Parse("{\"DATE_OUTPUT_FORMAT\": \"YYYY_MM_DD\"}");
            }
            else
                throw new Exception("Failed to get snowflake token \nRequest: " + _request + "\nResponse: " + _response);       
        }

        struct SnowRequestBody
        {
            public string statement;
            public string database;
            public string warehouse;
            public string role;
            public int timeout;
            public JObject parameters;
        }
        struct SnowResponse
        {
            public HttpStatusCode StatusCode;
            public string code;
            public string message;
            public string sqlState;
            public string statementHandle;
            public string data;
        }

        // Get result status code
        public string StatusCode { 
            get
            {
                return result.StatusCode.ToString();
            }
        }

        // Get result value by attribute label
        public string GetResult(string label)
        {
            foreach (System.Reflection.FieldInfo f in result.GetType().GetFields())
                if (f.Name == label)
                    return f.GetValue(result).ToString();
            throw new KeyNotFoundException("Key '" + label + "' not found in " + result.ToString());
        }

        /// <summary>
        /// Submit the request and process the response
        /// </summary>
        /// <param name="query">query to send</param>
        /// <param name="timeout"></param>
        /// <returns>StatusCode</returns>
        public HttpStatusCode SubmitQuery(string query, int timeout = 0)
        {
            Hashtable headers = new Hashtable();
            headers.Add("Authorization", "Bearer " + snowToken);
            headers.Add("Content-Type", "application/json");
            headers.Add("User-Agent", "omrtestautomation");
            headers.Add("X-Snowflake-Authorization-Token-Type", "KEYPAIR_JWT");

            reqBody.statement = query;
            if (timeout > 0) reqBody.timeout = timeout;

            var body = JsonConvert.SerializeObject(reqBody);
            Tuple<IRestResponse, String> response = _sender.GET_RestSharp(snowUrl, headers, "POST", body);

            result = ParseSnowApiResult(response.Item1);
            return result.StatusCode;
        }

        /// <summary>
        /// Submit a query and return the data, parsed into the standard query-result format
        /// </summary>
        /// <param name="query"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public string QuerySnowflake(string query, int timeout=0)
        {
            SubmitQuery(query, timeout);
            return result.data;
        }

        /// <summary>
        /// Parse the Rest Response for a sucessful or failed request
        /// </summary>
        /// <param name="resp"></param>
        /// <returns>Key elements of the response that we are interested in</returns>
        SnowResponse ParseSnowApiResult(IRestResponse resp)
        {
            SnowResponse parsedResult = new SnowResponse();
            parsedResult.StatusCode = resp.StatusCode;
            JObject result = JObject.Parse(resp.Content.ToString());
            try
            {
                parsedResult.code = result["code"].ToString();
                parsedResult.message = result["message"].ToString();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse response content: " + resp.Content.ToString(), e);
            }
            if (resp.StatusCode != HttpStatusCode.Unauthorized)
            {
                parsedResult.sqlState = result["sqlState"].ToString();
                parsedResult.statementHandle = result["statementHandle"].ToString();
            }
            if (resp.StatusCode == HttpStatusCode.OK)
                parsedResult.data = ParseSnowApiData(resp.Content.ToString());
            else
                parsedResult.data = "Error";
            return parsedResult;
        }

        /// <summary>
        /// Parse the repsonse from Snowflake Rest Api into the format expected
        /// by the results checker
        /// </summary>
        /// <param name="response">Response body from Snowflake Api</param>
        /// <returns>Response data in the format of the expected result</returns>
        public string ParseSnowApiData(string response)
        {
            List<string> parsed_result = new List<string>();
            try
            {
                JObject result = JObject.Parse(response);
                JObject metadata = (JObject)result["resultSetMetaData"];
                int rowCount = (int)((JObject)((JArray)metadata["partitionInfo"]).First)["rowCount"];
                JArray columns = (JArray)metadata["rowType"];
                JArray data = (JArray)result["data"];
                for (int i = 0; i < rowCount; i++)
                {
                    List<string> parsedColKeyValue = new List<string>();
                    for (int col = 0; col < columns.Count; col++)
                    {
                        string thisColName = (string)columns[col]["name"];
                        JArray thisRecord = (JArray)data[i];
                        string thisColValue = thisRecord[col].ToString();
                        if ((string)columns[col]["type"] == "date")
                            thisColValue = thisColValue.Replace('_', '-');
                        parsedColKeyValue.Add(thisColName +":"+ thisColValue);
                    }
                    parsed_result.Add('{' + string.Join(',', parsedColKeyValue) + '}');
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse response resultSetMetaData or data\n" + response, e);
            }
            return '[' + string.Join(',', parsed_result) + ']';
        }

    }
}
