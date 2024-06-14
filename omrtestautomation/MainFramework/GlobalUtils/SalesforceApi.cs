using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit.Abstractions;
using RestSharp;
using System.Net;
using System.Threading;

namespace MainFramework.GlobalUtils
{
    public class SalesforceApi
    {
        const string dataservices = "services/data/v47.0";
        public string _scenarioString = String.Empty;
        public string _scenarioData = String.Empty;
        public string DataUrl, QueryUrl, ServiceUrl;
        public string SFNameSpace;
        public string token;
        SalesForceDataCollector sfdc;

        //ITestOutputHelper _output;
        List<string> native_clms = new List<string> { "id", "Id", "name", "Name", "DeveloperName", "Username" };
        List<string> native_tables = new List<string> { "Account", "RecordType", "User" };

        public SalesforceApi(TenantConfig t)
        {
            sfdc = new SalesForceDataCollector(t);
            token = sfdc.GET_SalesforceToken();
            ServiceUrl = sfdc._baseURL + "/services";
            DataUrl = sfdc._baseURL + dataservices + "/sobjects/";
            QueryUrl = sfdc._baseURL + dataservices +"/query/?q=";
            SFNameSpace = sfdc._nsprefix;
        }
        public SalesforceApi(string accesstoken, string orgURL, string sf_prefix)
        {
            sfdc = new SalesForceDataCollector(accesstoken, orgURL, sf_prefix);
            token = accesstoken;
            DataUrl = orgURL + dataservices + "/sobjects/";
            QueryUrl = orgURL + dataservices + "/query/?q=";
            SFNameSpace = sf_prefix;
        }

        public string AssumeUserIdentity(string username, string password)
        {
            sfdc.SET_SalesforceToken(username,password);
            token = sfdc.GET_SalesforceToken();
            return token;
        }

        private string buildPostBody(Object obj, string sfnamespace)
        {
            string _scenarioString = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            JObject obj2 = new JObject();
            obj2 = addSFNameSpaceToJSONBody(_scenarioString, sfnamespace);
            return obj2.ToString();
        }

        private string buildPostBody(Object obj)
        {
            JObject obj2 = JObject.Parse(JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            return obj2.ToString();
        }

        public JObject GetResponseBody(string strRequest, string requesttype, string strBody, HttpStatusCode expCode=HttpStatusCode.OK)
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

        public eToken GetEngineToken()
        {
            string trim_namespace = SFNameSpace.Trim('_');
            string tokenurl = ServiceUrl + "/apexrest" + (trim_namespace.Length==0 ? "" : '/'+trim_namespace) + "/api/v1/lexitoken";
            JObject responseBody = GetResponseBody(tokenurl, "GET", "NONE");
            return new eToken((string)responseBody.GetValue("token"), (string)responseBody.GetValue("refresh"),
                (string)responseBody.GetValue("lexiApiKey"), (string)responseBody.GetValue("endpoint"));
        }

        public JObject Get_Query(string query)
        {
            JObject itemData = GetResponseBody(QueryUrl + query, "GET", "NONE");
            JArray ArrData = JArray.Parse(itemData["records"].ToString());
            JObject joReturn = JObject.Parse(ArrData[0].ToString());
            return joReturn;
        }

        public String Post_Object(string tablename, Object obj, string sfnamespace = null)
        {
            string effectiveNameSpace = sfnamespace ?? SFNameSpace;
            try
            {
                JObject responseBody = GetResponseBody(DataUrl + effectiveNameSpace + tablename, "POST", buildPostBody(obj, effectiveNameSpace), HttpStatusCode.Created);
                return (string)responseBody.GetValue("id");
            } catch (Exception e)
            {
                throw new Exception("Failed with request: " + buildPostBody(obj, effectiveNameSpace) + "\n With exception during POST: " + e.Message);
            }
        }
        public void Post_Object_NoId(string tablename, Object obj, string sfnamespace = null)
        {
            string effectiveNameSpace = sfnamespace ?? SFNameSpace;
            try
            {
                JObject responseBody = GetResponseBody(DataUrl + effectiveNameSpace + tablename, "POST", buildPostBody(obj, effectiveNameSpace), HttpStatusCode.Created);
            }
            catch (Exception e)
            {
                throw new Exception("Failed with request: " + buildPostBody(obj, effectiveNameSpace) + "\n With exception during POST: " + e.Message);
            }
        }

        /// <summary>
        /// CheckRecordExists looks for the 'known_value' in the 'known_column'. Both committed
        /// and scenario tables are checked. If not found the tables are polled for up to 60 seconds.
        /// This method is used when we post data into sfdc. We may need to check if the referenced object 
        /// data is ready in sfdc, or we may need to get ids that are dynamically generated by sfdc.
        /// </summary>
        /// <param name="objname">The object to lookup</param>
        /// <param name="known_value">The value to lookup on</param>
        /// <param name="required_column_value">The column we want</param>
        /// <param name="known_column">The column to lookup on</param>
        /// <param name="scenarioid"></param>
        /// <returns>required_column_value for the record with the known value, otherwise not found</returns>
        public string CheckRecordExists(string objname, string known_value, string required_column_value= "SOMTerritoryId__c", string known_column = "UniqueIntegrationId__c", string scenarioid=null)
        {
            int max_wait_time_sec = 60; int elapsed = 0; string lookup_colmn = "Id";
            string obj_id; string scenario_obj_id ; bool found;
            string trimmed_object_name = objname.Replace("__c", "").Replace("OM", "");
            string committed_object_name = "OM" + trimmed_object_name + "__c";
            string scenario_object_name = "OMScenario" + trimmed_object_name + "__c";
            obj_id = LookupValue(committed_object_name, lookup_colmn, known_column, known_value);
            scenario_obj_id = LookupValue(scenario_object_name, lookup_colmn, known_column, known_value, scenarioid);
            found = !(obj_id.StartsWith(SalesForceDataCollector.not_found) && scenario_obj_id.StartsWith(SalesForceDataCollector.not_found) && obj_id != null);
            while (!found && elapsed < max_wait_time_sec)
            {
                Thread.Sleep(5 * 1000);
                elapsed += 5;
                obj_id = LookupValue(committed_object_name, lookup_colmn, known_column, known_value);
                scenario_obj_id = LookupValue(scenario_object_name, lookup_colmn, known_column, known_value, scenarioid);
                found = !(obj_id.StartsWith(SalesForceDataCollector.not_found) && scenario_obj_id.StartsWith(SalesForceDataCollector.not_found) && obj_id != null);
            }
            Console.WriteLine("DEBUG: CheckRecordExists for " + trimmed_object_name +
                " " + known_column + "=" + known_value + (found ? " found" : " NOT found"));
            if (!obj_id.StartsWith(SalesForceDataCollector.not_found))
            {
                Console.WriteLine("\t Committed object:" + committed_object_name + " Id:" + obj_id);
                return LookupValue(committed_object_name, required_column_value, known_column, known_value);
            }
            else if (!scenario_obj_id.StartsWith(SalesForceDataCollector.not_found))
            {
                Console.WriteLine("\t Scenario object:" + scenario_object_name + " Id:" + scenario_obj_id);
                return LookupValue(scenario_object_name, required_column_value, known_column, known_value); ;
            }
            else
                return SalesForceDataCollector.not_found;
        }
        public String Post_NativeObject(string objectname, Object obj)
        {
            try
            {
                JObject responseBody = GetResponseBody(DataUrl + objectname, "POST", obj.ToString(), HttpStatusCode.Created);
                return (string)responseBody.GetValue("id");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        public void Patch_Object(string tablename, Object obj, string strId)
        {
            GetResponseBody(DataUrl + SFNameSpace + tablename + "/" + strId, "PATCH", buildPostBody(obj, SFNameSpace), HttpStatusCode.NoContent);
        }

        public void Patch_OCE_Objects(string tablename, Object obj, string strId)
        {
            GetResponseBody(DataUrl + tablename + "/" + strId, "PATCH", buildPostBody(obj), HttpStatusCode.NoContent);
        }

        public void Delete_Object(string tablename, string strId)
        {
            GetResponseBody(DataUrl + SFNameSpace + tablename + "/" + strId, "DELETE", "NONE", HttpStatusCode.NoContent);
        }

        public JObject addSFNameSpaceToJSONBody(string strBody, string sfnamespace)
        {
            JObject obj = JObject.Parse(strBody);
            JObject obj1 = new JObject();

            foreach (var x in obj)
            {
                if (native_clms.Contains(x.Key.ToString()))
                {
                    obj1.Add(x.Key, x.Value.ToString());
                }
                else
                {
                    obj1.Add(sfnamespace + x.Key, x.Value);
                }

            }
            return obj1;
        }

        /// <summary>
        /// Looks up the value in the column SOMXxxId__c in the OMScenarioXxx_c table, for the record wih
        /// 'known_clm' = 'known_value'. By default the known column is "Id"
        /// </summary>
        /// <param name="tablename">Scenario table OMScenarioXxx_c</param>
        /// <param name="known_value">The value to lookup on, usually the Id for the record</param>
        /// <param name="known_clm">The column for the lookup, defaults to Id</param>
        /// <returns></returns>
        public string ScenarioSOMId(string tablename, string known_value, string known_clm="Id")
        {
            string SOM_clm = "S" + tablename.Insert(tablename.Length - "__c".Length, "Id").Remove("OM".Length, "Scenario".Length);
            return LookupValue(tablename, SOM_clm, known_clm, known_value);
        }

        /// <summary>
        /// Performs a lookup on OMR tables, OMR Scenario tables, Native sfdc tables. Returns the value of the column
        /// 'lookup' for the row with column 'known' containing value 'kown_value'.
        /// If the lookup is on an OMScenarioXxx table, the scenario id should be provided, to ensure that we return
        /// the record for this scenario (uncommitted scenarios can have the same data).
        /// </summary>
        /// <param name="tablename">table</param>
        /// <param name="lookup">column holding the value to return</param>
        /// <param name="known">column holding the known value</param>
        /// <param name="known_value">value to lookup on</param>
        /// <param name="scenarioid">scenario to look for if we are looking up on a scenario table</param>
        /// <returns>The value of the lookup column</returns>
        public string LookupValue(string tablename, string lookup, string known, string known_value, string scenarioid=null)
        {
            string lookup_clm = native_clms.Contains(lookup) ? lookup : SFNameSpace + lookup;
            string known_clm = native_clms.Contains(known) ? known : SFNameSpace + known;
            string sc_column = SFNameSpace + "OMScenarioId__c";
            string lookup_table = native_tables.Contains(tablename) ? tablename : SFNameSpace + tablename;
            string query = "select " + lookup_clm + " from " + lookup_table + " where " + known_clm + " = '" + known_value + "'"
                + ( scenarioid == null ? "" : " and " + sc_column + " = '" + scenarioid + "'" )
                + " limit 1";
            return sfdc.GetValue(token, query, lookup_clm);
        }
        public string LookupValueNoSubs(string lookup_table, string lookup_clm, string known_clm, string known_value)
        {
            string query = "select " + lookup_clm + " from " + lookup_table + " where " + known_clm + " = '" + known_value + "' limit 1";
            return sfdc.GetValue(token, query, lookup_clm);
        }

        public string GetSFUserId(string orgUsername)
        {
            string query = "select Id from User where Username = '" + orgUsername + "'";
            return sfdc.GetValue(token, query, "Id");
        }
    }
}
