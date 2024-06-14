using System;
using System.Collections.Generic;
using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MainFramework.GlobalUtils
{
    public class DynamoHelper
    {
        private AmazonDynamoDBClient _client;
        private DynamoDBContext _context;
        private string table_env_prefix;
        globalCodes awsCreds;

        public DynamoHelper(string credsFile)
        {
            awsCreds = new globalCodes(credsFile);
            var credentials = new BasicAWSCredentials(awsCreds["awsAccessKey"], awsCreds["awsSecretKey"]);
            _client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
            table_env_prefix = "MASTER_";
        }
        public DynamoHelper(string credsFile, string targetEnv)
        {
            awsCreds = new globalCodes(credsFile);
            EnvConfig env = new EnvFixture(targetEnv).Config;
            var credentials = new BasicAWSCredentials(awsCreds["awsAccessKey"], awsCreds["awsSecretKey"]);
            RegionEndpoint reg;
            switch (env.AwsRegion) { 
                case "us-east-1": reg = RegionEndpoint.USEast1; break;           // N Virginia (default)
                case "eu-west-1": reg = RegionEndpoint.EUWest1; break;           // Ireland
                case "eu-west-2": reg = RegionEndpoint.EUWest2; break;           // London
                case "ap-northeast-1": reg = RegionEndpoint.APNortheast1; break; // Tokyo
                case "ap-southeast-1": reg = RegionEndpoint.APSoutheast1; break; // Singapore
                case "ca-central-1": reg = RegionEndpoint.CACentral1; break;     // Canada
                default: reg = RegionEndpoint.USEast1; break;
            }
            _client = new AmazonDynamoDBClient(credentials, reg);
            table_env_prefix = TablePrefixFromEnv(targetEnv) + "_";
            _context = new DynamoDBContext(
                _client, 
                new DynamoDBContextConfig(){ TableNamePrefix = table_env_prefix }
                );
        }
        private string TablePrefixFromEnv(string targetEnv)
        {
            if (targetEnv.ToLower().Contains("preprod")) return "PREPROD";
            else if (targetEnv.ToLower().Contains("prod")) return "PROD";
            else if (targetEnv.ToLower().Contains("staging")) return "PREPROD";
            else return targetEnv.ToUpper();
        }
        public async Task<Dictionary<string, AttributeValue>> GetDynamoByKey(string strField, string strFieldValue, string strTableName)
        {
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
            {
                {strField, new AttributeValue { S = strFieldValue} }
            };
            GetItemRequest req = new GetItemRequest
            {
                TableName = table_env_prefix + strTableName,
                Key = key
            };
            var result = await _client.GetItemAsync(req);
            return result.Item;
        }
        public async Task<HttpStatusCode> UpdateDynamoByKey(string strField, string strFieldValue, string strTableName, string strKeyValue, string strKey = "TenantId" )
        {

            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>
            {
                {strKey, new AttributeValue { S = strKeyValue} }
            };
            Dictionary<string, AttributeValueUpdate> update_items = new Dictionary<string, AttributeValueUpdate>
            {
                {strField,
                    new AttributeValueUpdate {
                        Action = AttributeAction.PUT,
                        Value = new AttributeValue { S = strFieldValue}
                    }
                }
            };

            var request = new UpdateItemRequest
            {
                TableName = table_env_prefix + strTableName,
                Key = key,
                AttributeUpdates = update_items
            };

            var result = await _client.UpdateItemAsync(request);
            return result.HttpStatusCode;
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
                TableName = table_env_prefix + strTableName,
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

        public string CollectSnowFlakeDetails(string strTenantId)
        {

            var scanConditions = new List<ScanCondition>();
            ScanCondition sc = new ScanCondition("TenantId",
                                        ScanOperator.Equal,
                                        strTenantId);
            scanConditions.Add(sc);

            var response = _context.ScanAsync<OMRTenantConfigurations>(scanConditions).GetRemainingAsync().Result;

            var snowflakeConnectionString = response[0].Snowflake.ConnectionString;
            var snowflakeRoleName = response[0].Snowflake.SnowflakeRoleName;
            var warehouse = response[0].Snowflake.Warehouses.ComputeWarehouseName;

            return snowflakeConnectionString.ToString() + "warehouse=" + warehouse.ToString() + ";role=" + snowflakeRoleName.ToString() + ";";

        }

    }
}
