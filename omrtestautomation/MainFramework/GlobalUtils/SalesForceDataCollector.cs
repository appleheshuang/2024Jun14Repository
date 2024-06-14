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

namespace MainFramework.GlobalUtils
{
    public class SalesForceDataCollector
    {
        public string _baseURL, _namespace, _nsprefix;
        private string _clientID, _clientSecret;
		private string _userName, _passWord;
		private RequestSender _send;
        private SnowFlakeHelper _sfHelp;
        //private ITestOutputHelper _output;
        private string _sftoken;
        private const string data_services = "/services/data/v47.0/";
        private const string auth_services = "/services/oauth2/token";
        private string _queryURL;
        public const string not_found = "Not found";
        public const string index_out_of_range = "Index was out of range";
        public SalesForceDataCollector(string baseURL, string clientID, string clientSecret, string userName, string passWord, string nameSpace = "OCEOTEST") {
            _baseURL = baseURL;
            _clientID = clientID;
            _clientSecret = clientSecret;
            _userName = userName;
            _passWord = passWord;
			_namespace = nameSpace == null ? "" : nameSpace.Trim('_');
            _nsprefix = prefixNamespace(_namespace);
            _send = new RequestSender();
            _sfHelp = new SnowFlakeHelper();
            SET_SalesforceToken();
            _queryURL = _baseURL + data_services + "query/?q=";
        }
        public SalesForceDataCollector(string token, string baseURL, string nameSpace = "OCEOTEST")
        {
            _baseURL = baseURL;
            _namespace = nameSpace == null ? "" : nameSpace.Trim('_');
            _nsprefix = prefixNamespace(_namespace);
            _send = new RequestSender();
            _sfHelp = new SnowFlakeHelper();
            _sftoken = token;
            _queryURL = _baseURL + data_services + "query/?q=";
        }
        public SalesForceDataCollector(TenantConfig config)
		{
			_baseURL = config.orgURL;
			_clientID = config.clientID;
			_clientSecret = config.clientSecret;
			_userName = config.orgUserName;
			_passWord = config.orgPassword;
			_namespace = (config.sfnamespace == null || config.sfnamespace == "") ? "" : config.sfnamespace.Trim('_');
            _nsprefix = prefixNamespace(_namespace);
            _send = new RequestSender();
			_sfHelp = new SnowFlakeHelper();
            SET_SalesforceToken();
            _queryURL = _baseURL + data_services + "query/?q=";
        }

        private string prefixNamespace(string sfnamespace)
        {
            if (sfnamespace != "" && sfnamespace.LastIndexOf("__") < sfnamespace.Length - 2)
                return sfnamespace + "__";
            else
                return sfnamespace;
        }

        public string GET_SalesforceToken() {
            return _sftoken;
        }
        public void SET_SalesforceToken(string username = null, string password = null)
        {
            //Arrange
            var strRequest = _baseURL + auth_services;
            Hashtable headers = new Hashtable();
                headers.Add("content-type", "application/x-www-form-urlencoded");
            var strFormData = string.Format("grant_type=password&client_id={0}&client_secret={1}&username={2}&password={3}",
                HttpUtility.UrlEncode(_clientID),
                HttpUtility.UrlEncode(_clientSecret),
                HttpUtility.UrlEncode(username ?? _userName),
                HttpUtility.UrlEncode(password ?? _passWord));
            //Act
            Tuple<WebResponse, String> responsTuple = _send.RETURN_WebResponse(strRequest, "POST", headers, "NONE", strFormData);
            //Apply
            var body = responsTuple.Item2;
            try
            {
                JObject jBody = JObject.Parse(body);
                _sftoken = jBody["access_token"].ToString();
            }
            catch (Exception e)
            {
                string error = "Failed to get salesforce access_token.\n\tRequest body = " + strFormData + "\n\tResponse body = " + body;
                throw new Exception(error, e);
            }
        }

        public Dictionary<string, List<string>> GET_Salesforce_ObjectsNFields(string token, string allowed = "") {
            //Arrange
            var strReqObjects = _baseURL + data_services +"sobjects";
            JObject jsObjects = GET_DefaultWebResponse(token, strReqObjects);
            var ArrSObjects = jsObjects["sobjects"];
            Dictionary<string, List<string>> dicObjFields = new Dictionary<string, List<string>>();
            //Act
            foreach (var item in ArrSObjects)
            {
                if (item["createable"].ToString() == "True")
                {
                    if (item["Name"].ToString().Substring(0, 2) == "OM" && item["Name"].ToString().Substring(0, 3) != "OMB"
                        && /**item["Name"].ToString().Contains("Field") == false &&**/ item["Name"].ToString().Contains("Attribute") == false
                        && item["Name"].ToString().Contains("Role") == false && item["Name"].ToString().Contains("Settings") == false
                        //&& ( item["Name"].ToString().Contains("Hierarchy") == false || allowed.Contains("Hierarchy") == true )
                        //&& ( item["Name"].ToString().Contains("History") == false || allowed.Contains("History") == true)
                        && (item["Name"].ToString().Contains("Share") == false || allowed.Contains("Share") == true)
                        && item["Name"].ToString().Contains("API") == false /**&& item["Name"].ToString().Contains("Assignment") == false**/
                        && item["Name"].ToString() != "OMScenarioTerritorySalesForce__c" && item["Name"].ToString() != "OMRuleOption__c"
                        && item["Name"].ToString() != "OMRuleField__c"
                        )
                    {
                        var strTable = item["Name"].ToString();
                        //GET THE FIELDS
                        List<string> fieldList = new List<string>();
                        var strTableReq = strReqObjects + "/" + strTable + "/describe";
                        JObject itemDesc = GET_DefaultWebResponse(token, strTableReq);
                        var ArrFields = itemDesc["fields"];
                        foreach (var jtem in ArrFields)
                        {
                            var fieldName = jtem["Name"].ToString();

                            if (fieldName != "Id" && fieldName != "OwnerId" && fieldName != "SystemModstamp" && fieldName != "LastViewedDate"
                            && fieldName != "LastReferencedDate" && fieldName != "IsDeleted" && fieldName.Contains("__c") == true)

                            {
                                fieldList.Add(fieldName);
                            }

                        }
                        dicObjFields.Add(strTable, fieldList);
                    }
                }
            }
            //Apply
            return dicObjFields;
        }

        public Dictionary<string, string> BUILD_SalesForceQuery(Dictionary<string, List<string>> dicObjFields) {
            //Arrange
            Dictionary<string, string> dicQuery = new Dictionary<string, string>();
            //Act
            foreach (var item in dicObjFields)
            {
                var strTableName = item.Key;
                var strFields = item.Value;
                string sqlQuery = "SELECT ";
                foreach (var l in strFields)
                {
                    sqlQuery = sqlQuery + l + ", ";
                }
                sqlQuery = sqlQuery + "FROM " + strTableName;
                sqlQuery = sqlQuery.Replace(", FROM", " FROM");
                dicQuery.Add(strTableName, sqlQuery);
            }
            //Apply
            return dicQuery;
        }

        public void CREATE_SalesforceCSVData(Dictionary<string, string> dicQuery, Dictionary<string, List<string>> dicObjFields, string token, string strFilePathFixed){
            //Arrange
            var host = _baseURL + "/services/data/v47.0/";
            //Act
            foreach (var item in dicQuery)
            {
                var strTableName = item.Key;
                var strQuery = item.Value;
                var strDataReq = host + "query/?q=" + strQuery;
                JObject itemData = GET_DefaultWebResponse(token, strDataReq);

                List<string> fieldList = dicObjFields[strTableName];
                var recordCount = System.Convert.ToInt32(itemData["totalSize"].ToString());
                JArray ArrData = new JArray(itemData["records"]);

                List<object> salesForceData = new List<object>();
                salesForceData.Add(fieldList);

                var strFileName = String.Empty;
                var strFileDir = strFilePathFixed;
                var strFilePath = strFileDir + strTableName + ".csv";

                foreach (var arr in ArrData)
                {
                        foreach (var x in arr){
							List<string> innerList = new List<string>();
							foreach (var l in fieldList){
                                innerList.Add(x[l].ToString());
                            }
                            salesForceData.Add(innerList);
                        }
                }
                //Apply
                if (recordCount > 0)
					_sfHelp.LOAD_toCSVFile(strFilePath, ",", salesForceData);
            }
        }

        public void CREATE_SalesforceInsertData(Dictionary<string, string> dicQuery, Dictionary<string, List<string>> dicObjFields, string token, string strFilePathFixed)
        {
            //Arrange
            var host = _baseURL + "/services/data/v47.0/";
            //Act
            foreach (var item in dicQuery)
            {
                var strTableName = item.Key;
                var strQuery = item.Value;
                var strDataReq = host + "query/?q=" + strQuery;
                JObject itemData = GET_DefaultWebResponse(token, strDataReq);

                List<string> fieldList = dicObjFields[strTableName];
                var recordCount = System.Convert.ToInt32(itemData["totalSize"].ToString());
                JArray ArrData = new JArray(itemData["records"]);

                var strFileName = String.Empty;
                var strFileDir = strFilePathFixed;
                var strFilePath = strFileDir + strTableName + ".sql";
                var entries = "INSERT into " + strTableName + " ";

                entries = entries + "(";
                foreach(var cols in fieldList)
                {
                    entries = entries + cols + ",";
                }
                entries = entries + ")";
                entries = entries.Replace(",)", ") VALUES ");
                entries = entries + System.Environment.NewLine;

                foreach (var arr in ArrData)
                {
                    if (recordCount > 0)
                    {
                        foreach (var keys in arr)
                        {
                            entries = entries + "(";
                            foreach (var l in fieldList)
                            {
                                entries = entries + "'" + keys[l].ToString() + "',";
                            }
                            entries = entries + ")";
                            entries = entries.Replace("',)", "'),");
                            entries = entries + System.Environment.NewLine;
                        }
                    }
                }
                entries = entries + ";";
                entries = entries.Replace(")," + System.Environment.NewLine + ";", ");");
                System.IO.File.WriteAllText(strFilePath, entries);
            }
        }

        public void COLLECT_JsonResponses(Dictionary<string, string> dicQuery, Dictionary<string, List<string>> dicObjFields, string token, string strFilePathFixed)
        {
            //Arrange
            var host = _baseURL + "/services/data/v47.0/";
            //Act
            foreach (var item in dicQuery)
            {
                var strTableName = item.Key;
                var strQuery = item.Value;
                var strDataReq = host + "query/?q=" + strQuery;
                JObject itemData = GET_DefaultWebResponse(token, strDataReq);
                var entries = JsonConvert.SerializeObject(itemData, Formatting.Indented);
                var strFileDir = strFilePathFixed;
                var strFilePath = strFileDir + strTableName + ".json";

                System.IO.File.WriteAllText(strFilePath, entries);
            }
        }

        public JObject GET_DefaultWebResponse(string token, string strRequest)
        {
            RequestSender _sendObj = new RequestSender();
            Hashtable headers2 = new Hashtable();
            headers2.Add("content-type", "application/json");
            headers2.Add("Authorization", "Bearer " + token);
            Tuple<WebResponse, String> responsTupleObj = _sendObj.RETURN_WebResponse(strRequest, "GET", headers2, "NONE", "NONE");
            var sObjects = responsTupleObj.Item2;
            try {
                JObject jsObjects = JObject.Parse(sObjects.ToString());
                return jsObjects;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing response \n\t request={1}\n\t response={2}",strRequest, sObjects.ToString());
                throw e;
            }

        }

        public void COLLECT_CSVs(string token, string strFilePath, string allowed_tables="")
        {
            //Collect Objects and Fields
            Dictionary<string, List<string>> objNFields = GET_Salesforce_ObjectsNFields(token, allowed_tables);
            //Create Queries
            Dictionary<string, string> objQueries = BUILD_SalesForceQuery(objNFields);
            //Create CSV Files
            CREATE_SalesforceCSVData(objQueries, objNFields, token, strFilePath);
        }

        public void COLLECT_ObjectAndFields(string token, string strFilePath)
        {
            Dictionary<string, List<string>> objNFields = GET_Salesforce_ObjectsNFields(token);
            string json = JsonConvert.SerializeObject(objNFields, Formatting.Indented);
            System.IO.File.WriteAllText(strFilePath, json);
        }

        public void CREATE_Queries(string token, string strFilePath)
        {
            Dictionary<string, List<string>> objNFields = GET_Salesforce_ObjectsNFields(token);
            Dictionary<string, string> objQueries = BUILD_SalesForceQuery(objNFields);
            string json = JsonConvert.SerializeObject(objQueries, Formatting.Indented);
            System.IO.File.WriteAllText(strFilePath, json);
        }

        public void CREATE_Inserts(string token, string strFilePath)
        {
            //Collect Objects and Fields
            Dictionary<string, List<string>> objNFields = GET_Salesforce_ObjectsNFields(token);
            //Create Queries
            Dictionary<string, string> objQueries = BUILD_SalesForceQuery(objNFields);
            //Create Insert Files
            CREATE_SalesforceInsertData(objQueries, objNFields, token, strFilePath.ToString());
        }

        public void COLLECT_Responses(string token, string strFilePath)
        {
            //Collect Objects and Fields
            Dictionary<string, List<string>> objNFields = GET_Salesforce_ObjectsNFields(token);
            //Create Queries
            Dictionary<string, string> objQueries = BUILD_SalesForceQuery(objNFields);
            //Create Response Files
            COLLECT_JsonResponses(objQueries, objNFields, token, strFilePath);
        }

        public void COLLECT_CSV_DevCodeBase(string token, string strFilePath)
        {
            ConnectSalesForce cs = new ConnectSalesForce();
            SalesforceFunctions sff = new SalesforceFunctions(ConnectSalesForce.sfdcCredentials);

            //Collect Objects and Fields
            Dictionary<string, List<string>> objNFields = GET_Salesforce_ObjectsNFields(token);
            //Create Queries
            Dictionary<string, string> objQueries = BUILD_SalesForceQuery(objNFields);

            foreach (var item in objQueries)
            {
                string queryName = item.Key;
                string query = item.Value;
                sff.extractData(query, queryName, strFilePath);
            }
        }

        public string GetValue(string token, string strQuery, string colName)
        {
			var host = _baseURL + "/services/data/v47.0/";
			var strDataReq = host + "query/?q=" + strQuery;
            JObject itemData;
            try
            {
			    itemData = GET_DefaultWebResponse(_sftoken, strDataReq);
            }
            catch (Exception e)
            {
                return not_found + ": Bad request \n Request=" + strDataReq + "\n" + e.Message;
            }
            JArray entries = (JArray)itemData["records"];
            try
            {
                JObject records = (JObject)entries[0];
                return records[colName].ToString();
            }
            catch (Exception e)
            {
                return not_found+ ": Element " + colName + " in returned data set " + itemData.ToString() + "\n Request=" + strDataReq + "\n" + e.Message;
            }
        }

        public Dictionary<string, string> COLLECT_SalesforceIDs(List<string> obj_to_purge = null)
        {
            //Arrange
            var strReqObjects = _baseURL + "/services/data/v47.0/sobjects";
            JObject jsObjects = GET_DefaultWebResponse(_sftoken, strReqObjects);
            var ArrSObjects = jsObjects["sobjects"];
            List<string> lstTables = new List<string>();
            //Act
            foreach (var item in ArrSObjects)
            {
                if (item["createable"].ToString() == "True" && item["deletable"].ToString() == "True" && item["name"].ToString().Length >= 16)
                {
                    string trimmed_name = item["name"].ToString().Replace(_nsprefix, "").Replace("__c", "");
                    if (
                        item["name"].ToString().Substring(0, 16) == _nsprefix + "OM" &&
                        item["name"].ToString().Substring(0, 17) != _nsprefix + "OMB" &&
                        item["name"].ToString().Contains("Attribute") == false &&
                        item["name"].ToString().Contains("Settings") == false &&
                        item["name"].ToString().Contains("Role") == false &&
                        item["name"].ToString().Contains("API") == false &&
                        item["name"].ToString().Contains("Share") == false &&
                        item["name"].ToString().Contains("Tenant") == false &&
                        item["name"].ToString().Contains("Report") == false &&
                        item["name"].ToString().Contains("ApiCallout") == false &&
                        item["name"].ToString().Contains("RuleField") == false &&
                        (obj_to_purge == null || obj_to_purge.Count == 0 || obj_to_purge.Contains(trimmed_name))
                    )
                    {
                        var strTable = item["name"].ToString();
                        lstTables.Add(strTable);
                    }
                }
            }

            var host = _baseURL + "/services/data/v47.0/";
            //var allData = "Id,ObjectName\r\n";
            Dictionary<string, string> dicIds = new Dictionary<string, string>();
            foreach (var item in lstTables)
            {
                var strQuery = "SELECT Id from " + item;
                var strDataReq = host + "query/?q=" + strQuery;

                JObject itemData = GET_DefaultWebResponse(_sftoken, strDataReq);
                var recordCount = System.Convert.ToInt32(itemData["totalSize"].ToString());
                JArray ArrData = new JArray(itemData["records"]);

                if (recordCount > 0)
                {
                    foreach (var arr in ArrData)
                    {
                        JArray jArr = JArray.Parse(arr.ToString());
                        foreach (var i in jArr)
                        {
                            JObject joArr = JObject.Parse(i.ToString());
                            //allData = allData + joArr["Id"] + "," + item  + "\r\n";
                            dicIds.Add(joArr["Id"].ToString(), item);
                        }
                    }
                }

            }

            //if (uploadToCSV)
            //{
            //    var strAllFile = strFilePathFixed + strFileName;
            //    System.IO.File.WriteAllText(strAllFile, allData);
            //}

            return dicIds;

        }

        public void COLLECT_ObjectAndFieldsToCSV(string token, string strFilePath, string strDelimeter)
        {
            System.IO.File.Delete(strFilePath);
            Dictionary<string, List<string>> objNFields = GET_Salesforce_ObjectsNFields(token);
            foreach (var item in objNFields)
            {
                var strTblName = item.Key;
                strTblName = strTblName.Replace("__c", "");
                var lstCols = item.Value;
                var strLine = "";
                var strTblCols = "";
                var colVal = "";
                foreach (var cols in lstCols)
                {
                    colVal = cols.ToString();
                    colVal = colVal.Replace("__c", "");
                    strTblCols = strTblName + strDelimeter + colVal;
                    strLine = strLine + strTblCols + Environment.NewLine;
                }
                System.IO.File.AppendAllText(strFilePath, strLine);
            }
        }

        public JArray RUN_SalesforceQuery(string strQuery)
        {
            JObject itemData = GET_DefaultWebResponse(_sftoken, _queryURL + strQuery);
            int rec_count = int.Parse(itemData["totalSize"].ToString());

            int max_wait_time_sec = 20; int elapsed = 0;
            //On high loads, the time between SYNCing and the entry appearing in sf table could be non-zero. 
            //Lines 477-483 implements a 20 second grace period at 5 second intervals. Aborts early if the record is found.
            while (rec_count==0 && elapsed < max_wait_time_sec)
            {
                Thread.Sleep(5 * 1000);
                itemData = GET_DefaultWebResponse(_sftoken, _queryURL + strQuery);
                rec_count = int.Parse(itemData["totalSize"].ToString());
                elapsed += 5;
            }

            JArray joReturn = new JArray();
            if (rec_count == 0)
                return joReturn;
            else
            {
                JArray ArrData = JArray.Parse(itemData["records"].ToString());
                JObject record_i;
                for (int i= 0; i < rec_count;  i++ )
                    {
                        record_i = JObject.Parse(ArrData[i].ToString());
                        record_i.Remove("attributes");
                        joReturn.Add(record_i);
                    }

                return joReturn; 
            }
        }

        /// <summary>
        /// Method to generate the refresh token using access token
        /// </summary>
        /// <returns></returns>
        public JObject GET_EngineToken()
        {
            string _token = GET_SalesforceToken();
            var strRequest = _baseURL + "/services/apexrest/"+_namespace+"/api/v1/lexitoken";
            Hashtable headers = new Hashtable();
            headers.Add("Authorization", "Bearer " + _token);
            Tuple<WebResponse, String> responsTuple = _send.RETURN_WebResponse(strRequest, "GET", headers, "NONE", "NONE");
            return JObject.Parse(responsTuple.Item2);
        }

        /// <summary>
        /// Get output data in a dictionary
        /// </summary>
        /// <param name="strQuery"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetSalesForceData(string strQuery)
        {
            var strDataReq = _queryURL + strQuery;
            JObject itemData = GET_DefaultWebResponse(GET_SalesforceToken(), strDataReq);
            JArray entries = (JArray)itemData["records"];
            JObject records = (JObject)entries[0];
            var objDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(records.ToString());
            return objDict;
        }
    }
}
