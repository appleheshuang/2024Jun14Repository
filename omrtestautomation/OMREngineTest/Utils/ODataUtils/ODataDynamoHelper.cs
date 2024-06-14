using System;
using System.Collections.Generic;
using System.Text;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ODataTest.ODataUtils
{
    public class ODataDynamoHelper
    {
        private AmazonDynamoDBClient _client;
        public ODataDynamoHelper(string strAccessKey, string strSecretKey)
        {
            var credentials = new BasicAWSCredentials(strAccessKey, strSecretKey);
            _client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
        }
        public async Task<Dictionary<string, AttributeValue>> GetDynamoByKey(string strField, string strFieldValue, string strTableName)
        {

            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
            {
                {strField, new AttributeValue { S = strFieldValue} }
            };

            var request = new GetItemRequest
            {
                TableName = strTableName,
                Key = key
            };

            var result = await _client.GetItemAsync(request);

            Dictionary<string, AttributeValue> item = result.Item;
            return item;
        }

        public async Task<Dictionary<string, AttributeValue>> GetDynamoByFieldContains(string strField, string strFieldValue, string strTableName)
        {
            Dictionary<string, string> searchField = new Dictionary<string, string>
            {
                {"#"+strField, strField }
            };

            Dictionary<string, AttributeValue> searchValues = new Dictionary<string, AttributeValue>
            {
                {":"+strField, new AttributeValue { S = strFieldValue.ToLower()} }
            };

            var request = new ScanRequest
            {
                TableName = strTableName,
                ExpressionAttributeNames = searchField,
                ExpressionAttributeValues = searchValues,
                FilterExpression = "contains(#" + strField + ", :" + strField + ")"
            };

            var result = await _client.ScanAsync(request);

            Dictionary<string, AttributeValue> item = result.Items[0];
            return item;
        }

        public JObject parseDynamo(Dictionary<string, AttributeValue> result)
        {
            JObject resJO = new JObject();
            foreach (var keyValuePair in result)
            {
                var strVal = keyValuePair.Value.M;
                if (strVal.Count != 0)
                {
                    JObject innerJO = new JObject();
                    foreach (var item in strVal)
                    {
                        innerJO.Add(item.Key, item.Value.S);
                    }
                    resJO.Add(keyValuePair.Key, innerJO);
                }
                else
                {
                    var nonMapVal = keyValuePair.Value.S;
                    if (nonMapVal == null)
                    {
                        nonMapVal = keyValuePair.Value.N;
                    }
                    resJO.Add(keyValuePair.Key, nonMapVal);
                }

            }
            return resJO;
        }

        public JObject parseSnowflakeConnStr(string connstr)
        {
            String[] snowFlakeListVal = connstr.Split(";");
            JObject snowFlakeVal = new JObject();
            foreach (var item in snowFlakeListVal)
            {
                if (item != "")
                {
                    String[] valueList = item.Split("=");
                    snowFlakeVal.Add(valueList[0], valueList[1]);
                }
            }
            return snowFlakeVal;
        }

        //public JObject getFirstLevelNode(JObject resJO, string node)
        //{
        //    var snowFlakeConfig = resJO[node];
        //}

    }
}
