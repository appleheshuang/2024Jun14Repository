using System;
using Newtonsoft.Json.Linq;
using MainFramework.GlobalUtils;
using ODataTest.ODataUtils;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using MainFramework;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ODataTest.Tests
{
    public class ODataTestDefinition
    {
        //Updated tenantConfig file
        public static string tenantconfigfile = "smoketest-org.json";
        public static string stringFuncTestData = "ODATATEST_STRINGFUNC.csv";
        public static string perTableTest = "ODATATEST_STRINGFUNC.csv";

        public RequestSender _sender;
        public s3TenantFixture _tenant;
        public HTTPClient_AsyncRequest _httpClientDef;
        public ODataHelper _oHelp;

        public static Dictionary<string, AttributeValue> dynamoRes;
        public static JObject dynamoJO;
        public JObject _allConfigs;
        public SnowFlakeHelper _snowflake;

        public string connStr, tenantID, token, refreshtoken, xapikey, tokenEndpoint, username, password, baseURL, apiGateWay, _targetEnv;
        //private static readonly ITestOutputHelper _output;

        public ODataTestDefinition()
        {
            //Kelangan k lng ilagay dito ung 1 time actions
            _oHelp = new ODataHelper();
            _sender = new RequestSender();
            _httpClientDef = new HTTPClient_AsyncRequest();
            _snowflake = new SnowFlakeHelper();
            _tenant = new s3TenantFixture(tenantconfigfile);
            connStr = _tenant.Config.snowConnection;

            _targetEnv = (_tenant.Config.targetEnv == "master") ? "latest/" : _tenant.Config.targetEnv+"/";
            apiGateWay = _tenant.Config.endpoint+_targetEnv;
            xapikey = _tenant.Config.lexiApiKey;
            username = _tenant.Config.TenantUser;
            password = _tenant.Config.TenantPassword;
            tenantID = _tenant.Config.TenantId;
            baseURL = _tenant.Config.endpoint+_targetEnv+ "odata/" + tenantID + "/";
            
            token = _sender.GET_token(xapikey, "NONE", apiGateWay + "token", username, password, "token");
            refreshtoken = _sender.GET_token(xapikey, "NONE", apiGateWay + "token", username, password, "refresh");

            //Collect Metadata            
            _httpClientDef.SET_BaseAddress(baseURL);
            _httpClientDef.ADD_Accept("application/xml");
            _httpClientDef.ADD_Headers("x-api-key", xapikey);
            _httpClientDef.ADD_Authorization("Bearer", token);

            Task<string> taskRes = _httpClientDef.GET_AsyncRequest_ResponseOnly("$metadata");
            taskRes.Wait();
            string response = taskRes.Result.ToString();

            var formattedResponse = XDocument.Parse(response).ToString();
            var metadataFilePath = _oHelp.returnRootDir() + @"TestData\OData\metadata.xml";
            System.IO.File.WriteAllText(metadataFilePath, formattedResponse);
            
            //Create Post Schema from MetaData            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            var postSchema = "{\r\n";
            foreach (XmlNode xmlNode in doc.DocumentElement.ChildNodes[0].ChildNodes[0].ChildNodes)
            {
                var tblName = "";
                var schemaForm = "{\r\n";
                if (xmlNode.Attributes["Name"].Value.Substring(0, 2) == "OM")
                {
                    tblName = xmlNode.Attributes["Name"].Value;
                    XmlNodeList entityList = xmlNode.ChildNodes;
                    Random rnd = new Random();
                    foreach (XmlNode item in entityList)
                    {

                        if (item.Name == "Property")
                        {
                            var nameAttr = item.Attributes["Name"].Value;
                            var typeAttr = item.Attributes["Type"].Value;
                            switch (typeAttr)
                            {
                                case "Edm.DateTimeOffset":
                                    //Timestamp needs to be in the right format or else it will fail
                                    //For now we set it to current date
                                    schemaForm = schemaForm + "\"" + nameAttr + "\":\"dateNow\",\r\n";
                                    break;
                                case "Edm.Decimal":                                    
                                    schemaForm = schemaForm + "\"" + nameAttr + "\":" + rnd.Next(1, 100).ToString() + ",\r\n";
                                    //schemaForm = schemaForm + "\"" + nameAttr + "\":\"randomDecimal\",\r\n";
                                    break;
                                case "Edm.Int64":
                                    schemaForm = schemaForm + "\"" + nameAttr + "\":" + rnd.Next(1, 100).ToString() + ",\r\n";
                                    //schemaForm = schemaForm + "\"" + nameAttr + "\":\"randomDecimal\",\r\n";
                                    break;                                    
                                case "Edm.Boolean":
                                    schemaForm = schemaForm + "\"" + nameAttr + "\":true,\r\n";
                                    break;
                                case "Edm.Date":
                                    schemaForm = schemaForm + "\"" + nameAttr + "\":\"dateNow\",\r\n";
                                    break;
                                default:
                                    if (nameAttr == "SourceId")
                                    {
                                        schemaForm = schemaForm + "\"" + nameAttr + "\":\"ODATATEST\",\r\n";
                                    }
                                    else if (nameAttr == "DegreeYear" || nameAttr == "Year")
                                    {
                                        schemaForm = schemaForm + "\"" + nameAttr + "\":\"year\",\r\n";
                                    } else
                                    {
                                        schemaForm = schemaForm + "\"" + nameAttr + "\":\"strSuffix\",\r\n";
                                    }
                                    break;
                            }
                        }
                    }
                    schemaForm = schemaForm + "},\r\n";
                    schemaForm = "\"" + tblName + "\":" + schemaForm;
                    postSchema = postSchema + schemaForm;
                }
            }
            postSchema = postSchema + "}";
            postSchema = postSchema.Replace(",\r\n}", "\r\n}");
            var postSchemaFile = _oHelp.returnRootDir() + @"TestData\OData\postRequest_schema.json";
            JObject jPostSchema = JObject.Parse(postSchema);
            System.IO.File.WriteAllText(postSchemaFile, jPostSchema.ToString());

            //Delete Data            
            var deleteQuery = _oHelp.DELETE_testdata(_snowflake, connStr, stringFuncTestData);
            _snowflake.RUN_SnowCommands(connStr, deleteQuery);
            
        }

    }
}
