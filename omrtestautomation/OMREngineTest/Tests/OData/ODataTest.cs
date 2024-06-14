using System;
using System.Data;
using Xunit;
using Xunit.Abstractions;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework.GlobalUtils;
using System.Collections;
using ODataTest.Types;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using System.Xml;
using MainFramework.TestUtils;
using JsonDiffPatchDotNet;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using MainFramework;
using Amazon.Runtime.Internal.Transform;

namespace ODataTest.Tests
{
    //[Collection("OData Collection")]
    //[Trait("TestRailProjectId", "ProjectId")]//This is for parsing in the output xml and all the traits below
    //[Trait("TestRailSuiteId", "SuiteId")]
    //[Trait("TestRailSectionId", "SectionId")] 
    //[GroupTraits()] //so you cannot use this in here might be what we need because each test might not be inserted on a certain Section
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    public class ODataTest : IClassFixture<ODataTestDefinition>
    {
        public static ITestOutputHelper _output;
        public static ODataTestDefinition _odata;

        public ODataTest(ODataTestDefinition odata, ITestOutputHelper output)
        {
            _odata = odata;
            _output = output;
        }

        [Theory, TestPriority(01)]
        [InlineData("OMRegion", "GET")]
        public void TEST_EndpointResponse(string strRequest, string strMethod)
        {
            //Arrange
            Hashtable headers = new Hashtable();
            headers.Add("content-type", "application/json");
            headers.Add("Authorization", "Bearer " + _odata.token);
            headers.Add("x-api-key", _odata.xapikey);
            //Act
            Tuple<IRestResponse, String> responsTuple = _odata._sender.GET_RestSharp(_odata.baseURL + strRequest, headers, strMethod, "NONE");
            var response = responsTuple.Item1;
            var body = responsTuple.Item2;
            var responseStatus = response.StatusCode;
            //_output.WriteLine(body);
            //Assert
            _output.WriteLine("TEST response status: " + responseStatus);
            Assert.Equal("OK", responseStatus.ToString());
            _output.WriteLine("response: " + body);
            Assert.False(body.Contains("error"), "Error Was Found in the response");
            Assert.False(body.Contains("ERROR"), "Error Was Found in the response");
            Assert.False(body.Contains("Error"), "Error Was Found in the response");
        }


        [Fact, TestPriority(02)]
        public void CHECKMETADATA_ReferentialConstraint()
        {
            string metadataxml = System.IO.File.ReadAllText(_odata._oHelp.returnRootDir() + @"TestData\OData\metadata.xml");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(metadataxml);
            XmlNodeList elemlist = xmlDoc.GetElementsByTagName("ReferentialConstraint");

            var writeRes = "";
            for (int i = 0; i < elemlist.Count; i++)
            {
                var result = elemlist[i].Attributes;
                var strProperty = result["Property"].Value;
                var strRefProperty = result["ReferencedProperty"].Value;
                if (strProperty != strRefProperty)
                {
                    var sqlQuery = "SELECT * FROM PUBLIC.\"SchemaKey\" where \"ParentColumn\" = '\"" + strProperty + "\"' and \"ChildColumn\" = '\"" + strRefProperty + "\"' " +
                        "UNION " +
                        "SELECT* FROM PUBLIC.\"SchemaKey\" where \"ParentColumn\" = '\"" + strRefProperty + "\"' and \"ChildColumn\" = '\"" + strProperty + "\"';";
                    SnowFlakeHelper snowflake = new SnowFlakeHelper();
                    int numRows = snowflake.queryReturnCount(_odata.connStr, sqlQuery);
                    if (numRows == 0)
                    {
                        writeRes = writeRes + "ReferentialConstraint Property=\"" + strProperty + "\" ReferencedProperty=\"" + strRefProperty + "\"" + "\r\n";
                    }
                }
            }

            _output.WriteLine("MetaData Referential Constraint Comparison: ");
            _output.WriteLine("Expected : <NONE>  => " + "Actuals: " + writeRes);
            Assert.Equal(string.Empty, writeRes);

        }



        //TEST ALL tables *****************************************************************************************************************************
        [Theory, TestPriority(03)]
        [ClassData(typeof(OMRTableTest))]
        public void VANILLA_Request_Testing(string Uniqkey, string sqlQuery, string strRequest)
        {
            //Arrange
            //Run SQL Query****************************************************************************************************************************
            IDataReader reader = _odata._snowflake.CONN_EXEC_ToSnowflake(_odata.connStr, sqlQuery);

            var r = _odata._snowflake.Serialize(reader, Uniqkey);
            int sfCount = r.Count;
            string jsonContent = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);

            //Run OData request*************************************************************************************************************************
            CodeHelper codeHelp = new CodeHelper();
            Hashtable headers = codeHelp.setUpAllHeaders("application/json", _odata.xapikey, "Bearer " + _odata.token, _odata.refreshtoken);

            Tuple<IRestResponse, String> responsTuple = _odata._sender.GET_RestSharp(_odata.baseURL + strRequest, headers, "GET", "NONE");
            JObject jo = JObject.Parse(responsTuple.Item2);
            var bodyValue = jo["value"].ToString();

            int jCount = codeHelp.COUNT_json(bodyValue.ToString());

            var odataDict = _odata._snowflake.SerializeJSON(bodyValue.ToString(), Uniqkey);
            string ODataContent = JsonConvert.SerializeObject(odataDict, Newtonsoft.Json.Formatting.Indented);

            //Act
            JObject sfData = JObject.Parse(jsonContent);
            JObject odataJson = JObject.Parse(ODataContent);
            string diff = _odata._oHelp.CompareObjects(sfData, odataJson).ToString();

            //Assert
            _output.WriteLine("TEST count matching: " + sfCount.ToString() + " vs " + jCount.ToString());
            Assert.Equal(sfCount, jCount);
            _output.WriteLine("****COMPARE DATA CHECK****\r\n");
            _output.WriteLine("DIFFRESULT:" + diff);
            _output.WriteLine("SNOWFLAKE:\r\n" + jsonContent + "\r\n" + "ODATA:\r\n" + ODataContent);
            Assert.Equal(jsonContent, ODataContent);
            //Assert.Equal(string.Empty, diff);  
        }


        [Theory, TestPriority(04)]
        [ClassData(typeof(OMRStringFunctions))]
        //[ClassData(typeof(OMRStringFunctions_frdb))]
        public void STRING_Functions_Testing(string postRequest, string fieldToChange, string fieldValue, string strRequest, string sqlQuery, string postBody)
        {

            //Apply Data**************************************************************************************************
            Hashtable postHeaders = _odata._oHelp.buildDefaultHeaders("application/json", "Bearer " + _odata.token, _odata.xapikey);
            String strBody = "";
            var fields = fieldToChange.Split('-');
            if (fields.Length > 1)
            {
                strBody = _odata._oHelp.controlPostBody(postBody, fieldToChange, fieldValue);
                strBody = _odata._oHelp.removeFieldFromPostBody(strBody, fieldToChange);
                var values = fieldValue.Split('-');
                for (int item = 0; item < fields.Length; item++)
                {
                    strBody = _odata._oHelp.controlPostBody(strBody, fields[item], values[item]);
                }
            }
            else
            {
                strBody = _odata._oHelp.controlPostBody(postBody, fieldToChange, fieldValue);
            }

            Tuple<IRestResponse, String> postTuple = _odata._sender.GET_RestSharp(_odata.baseURL + postRequest, postHeaders, "POST", strBody);
            var postStatusCode = postTuple.Item1.StatusCode.ToString();
            _output.WriteLine("POST StatusCode: " + postStatusCode);


            if (postStatusCode == "OK")
            {
                //Arrange*****************************************************************************************************
                int jCount = 0;
                int sfCount = 0;

                string jDT = "";
                try
                {
                    IDataReader reader = _odata._snowflake.CONN_EXEC_ToSnowflake(_odata.connStr, sqlQuery);
                    //dt = new DataTable();
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    sfCount = dt.Rows.Count;
                    //sfCount = _snowflake.LOAD_ReaderToCSV(reader, expFilePath, "\t");
                    jDT = _odata._snowflake.CONVERT_DataTableToJSON(dt);

                }
                catch (Exception) { _output.WriteLine("ERROR collecting data from Snowflake"); }


                JObject jObj = new JObject();
                JArray ja = new JArray();
                Hashtable headers = _odata._oHelp.buildDefaultHeaders("application/json", "Bearer " + _odata.token, _odata.xapikey);
                try
                {
                    Tuple<IRestResponse, String> responsTuple = _odata._sender.GET_RestSharp(_odata.baseURL + strRequest, headers, "GET", "NONE");
                    var bodyValue = _odata._oHelp.getResponseParsedValue(responsTuple.Item2);
                    ja = JArray.Parse(bodyValue);
                    jCount = ja.Count;
                }
                catch (Exception) { _output.WriteLine("ERROR collecting OData"); }

                //Act**********************************************************************************************************         
                JArray expJArr = JArray.Parse(jDT);
                JObject expected = JObject.Parse(expJArr[0].ToString());

                Dictionary<string, string> dictObj = expected.ToObject<Dictionary<string, string>>();
                var expKeys = dictObj.Keys;
                foreach (var item in expKeys)
                {
                    if (
                        item != "CreatedDate" &&
                        item != "LastModifiedDate" &&
                        item.Contains("Date")
                        )
                    {
                        expected[item] = DateTime.Parse(expected[item].ToString()).ToString("yyyy-MM-dd");
                    }
                }

                JObject actual = JObject.Parse(ja[0].ToString());

                var diffRes = _odata._oHelp.CompareObjects(expected, actual).ToString();

                //Assert*******************************************************************************************************
                if (jCount == 0 || sfCount == 0)
                {
                    _output.WriteLine("PARAMETERS: " +
                        System.Environment.NewLine +
                        "Post Endpoint:" + postRequest + System.Environment.NewLine +
                        "Post Body: " + strBody + System.Environment.NewLine +
                        "OData Request: " + strRequest + System.Environment.NewLine +
                        "Snowflake Query: " + sqlQuery);
                    _output.WriteLine("No OData collected....");
                    Assert.True(false);
                }
                else
                {
                    _output.WriteLine("PARAMETERS: " +
                        System.Environment.NewLine +
                        "Post Endpoint:" + postRequest + System.Environment.NewLine +
                        "Post Body: " + strBody + System.Environment.NewLine +
                        "OData Request: " + strRequest + System.Environment.NewLine +
                        "Snowflake Query: " + sqlQuery);
                    _output.WriteLine("COUNT Comparison: Snowflakes: " + sfCount.ToString() + " vs OData: " + jCount.ToString());
                    Assert.Equal(sfCount, jCount);

                    _output.WriteLine("SNOWFLAKE RETURN: " + expected.ToString(Newtonsoft.Json.Formatting.Indented));
                    _output.WriteLine("ODATA RETURN: " + actual.ToString(Newtonsoft.Json.Formatting.Indented));
                    _output.WriteLine("DIFF RESULT:\r\n" + diffRes.ToString());
                    Assert.Equal(diffRes, String.Empty);
                }

            }
            else
            {
                _output.WriteLine("PARAMETERS: " + System.Environment.NewLine + postRequest + System.Environment.NewLine + strBody + System.Environment.NewLine + strRequest + System.Environment.NewLine + sqlQuery);
                _output.WriteLine("POSTING Data Failed!");
                Assert.True(false);
            }

        }

        [Theory, TestPriority(05)]
        [InlineData("VAccountAddress?$select=OMAccountId,AccountName,OMAccountAddressId&$filter = OMAccountId eq '10ACCTEXC'&$expand=OMAccount($select= Id)", "GET",
            "select a.\"OMAccountId\",a.\"AccountName\",a.\"OMAccountAddressId\",b.\"Id\"  from \"STATIC\".\"VAccountAddress\" a join  \"STATIC\".\"OMAccount\" b on a.\"OMAccountId\"=b.\"Id\" where a. \"OMAccountId\"='10ACCTEXC'")]
        public void TEST_ExpandFunction(string strRequest, string strMethod, string sqlQuery)
        {
            //Arrange
            Hashtable headers = new Hashtable();
            headers.Add("content-type", "application/json");
            headers.Add("Authorization", "Bearer " + _odata.token);
            headers.Add("x-api-key", _odata.xapikey);

            //Act
            Tuple<IRestResponse, String> responsTuple = _odata._sender.GET_RestSharp(_odata.baseURL + strRequest, headers, strMethod, "NONE");
            var response = responsTuple.Item1;
            var body = responsTuple.Item2;
            JArray ja = new JArray(); ;
            var bodyValue = _odata._oHelp.getResponseParsedValue(body);

            DataTable dt = new DataTable();
            int jsCount = 0;
            int sfCount = 0;
            if (response.StatusCode.ToString() == "OK")
            {
                ja = JArray.Parse(bodyValue);
                jsCount = ja.Count;
                try
                {
                    IDataReader reader = _odata._snowflake.CONN_EXEC_ToSnowflake(_odata.connStr, sqlQuery);
                    dt.Load(reader);
                    sfCount = dt.Rows.Count;
                }
                catch
                {
                    _output.WriteLine("ERROR collecting data from Snowflake");
                }

                //Convert Datatable to a nested Json
                var test = new Dictionary<string, object>();
                foreach (DataColumn column in dt.Columns)
                {
                    if (column.ColumnName == "Id")
                    {
                        test.Add("OMAccount", new Dictionary<string, string>
                        {
                            { column.ColumnName, dt.Rows[0][column.ColumnName].ToString() }
                        });
                    }
                    else
                    {
                        test.Add(column.ColumnName, dt.Rows[0][column.ColumnName].ToString());
                    }
                }
                string json = JsonConvert.SerializeObject(test, Newtonsoft.Json.Formatting.Indented);

                //Compare Json Objects
                JObject expected = JObject.Parse(json);
                JObject actual = JObject.Parse(ja[0].ToString());
                bool value = JToken.DeepEquals(expected, actual);

                //Assert*******************************************************************************************************
                if (jsCount == 0 || sfCount == 0 || !value)
                {
                    string error_log = ("Record count mismatch \nPARAMETERS: " + System.Environment.NewLine + "OData Request: " + strRequest + System.Environment.NewLine + "Snowflake Query: " + sqlQuery);
                    if (jsCount == 0 || sfCount == 0)
                    {
                        error_log += "\nCOUNT Comparison: Snowflakes: " + sfCount.ToString() + " vs OData: " + jsCount.ToString();
                        Assert.True(sfCount == jsCount, error_log);
                    }
                    else
                    {
                        error_log += "\nSNOWFLAKE RETURN: " + expected.ToString(Newtonsoft.Json.Formatting.Indented);
                        error_log += "\nODATA RETURN: " + actual.ToString(Newtonsoft.Json.Formatting.Indented);
                        Assert.True(false, error_log);
                    }
                }
            }
        }
    }
}
