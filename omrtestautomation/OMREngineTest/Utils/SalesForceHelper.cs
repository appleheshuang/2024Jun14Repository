using MainFramework.GlobalUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Salesforce.Common;
using Salesforce.Common.Models.Json;
using Salesforce.Force;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace OMREngineTest.Utils
{
    public class SalesForceHelper
    {
        private string _clientID;
        private string _clientSecret;
        private string _userName;
        private string _passWord;
        private string _baseURL;
        private string _namespace;
        private ForceClient _client;
        private AuthenticationClient _auth;
        public SalesForceHelper(string clientID, string clientSecret, string userName, string passWord, string baseURL, string sfnamespace)
        {
            _clientID = clientID;
            _clientSecret = clientSecret;
            _userName = userName;
            _passWord = passWord;
            _baseURL = baseURL;
            _namespace = sfnamespace;
            /**
             * Note that in packaged org or devHub - password is appended by security token
             * url is only required in scratch org as it is not a default token link
             **/
            var loginURL = _baseURL + "/services/oauth2/token";

            _auth = new AuthenticationClient();
            _auth.UsernamePasswordAsync(_clientID, _clientSecret, _userName, _passWord, loginURL.ToString()).Wait(); // the url part is only for scratch org and NOT on the dev hub org
            _client = new ForceClient(_auth.InstanceUrl, _auth.AccessToken, _auth.ApiVersion);


        }
        public void deleteSalesForceData(Dictionary<string, string> dicIds)
        {            
            Task<bool> del;
            //const int max_purges = 100;
            //Purge in order of deleting from child to parent
            Dictionary<string, string> objs_by_level;
            for (int level = 3; level >= 0; level--)
            {
                objs_by_level = filterByLevel(dicIds, level);
                //if (objs_by_level.Count > max_purges)
                //    throw new Exception("Max SalesForce deletes = " + max_purges + ", Actual # deletes = " + objs_by_level.Count);
                if (objs_by_level.Count > 0) { 

                    var sortedIds = objs_by_level.OrderByDescending(kv => kv.Key);
                    foreach (var item in sortedIds)
                    {
                        try
                        {
                            del = _client.DeleteAsync(item.Value, item.Key);
                            del.Wait();
                        }
                        catch (AggregateException)
                        {
                            continue;
                        }
                    }

                }
            }
        }

        public Dictionary<string, string> filterBySchema(Dictionary<string, string> kvp, string strSchema, string xnamespace=null)
        {
            string sfnamespace = xnamespace ?? _namespace;
            var search = new HashSet<string>();
            switch (strSchema)
            {
                case "OUTPUT":
                    //These tables are only snowf
                    //search.Add(sfnamespace + "OMAccountExclusion__c");
                    //search.Add(sfnamespace + "OMAccountExplicit__c");
                    //search.Add(sfnamespace + "OMAccountTerritory__c");
                    search.Add(sfnamespace + "OMGeographyTerritory__c");
                    search.Add(sfnamespace + "OMProductExclusion__c");
                    search.Add(sfnamespace + "OMProductExplicit__c");
                    search.Add(sfnamespace + "OMProductSalesForce__c");
                    search.Add(sfnamespace + "OMProductTerritory__c");
                    search.Add(sfnamespace + "OMSalesForceHierarchy__c");
                    search.Add(sfnamespace + "OMScenarioError__c");
                    search.Add(sfnamespace + "OMScenarioSummary__c");
                    search.Add(sfnamespace + "OMTerritoryHierarchy__c");
                    search.Add(sfnamespace + "OMTerritorySalesForce__c");
                    search.Add(sfnamespace + "OMUserAssignment__c");
                    break;
                case "STATIC":
                    search.Add(sfnamespace + "OMUserAddress__c");
                    search.Add(sfnamespace + "OMUserCommunication__c");
                    search.Add(sfnamespace + "OMUserContact__c");
                    search.Add(sfnamespace + "OMUserDependent__c");
                    search.Add(sfnamespace + "OMUserDriver__c");
                    search.Add(sfnamespace + "OMUserDriverIncident__c");
                    search.Add(sfnamespace + "OMUserEducation__c");
                    search.Add(sfnamespace + "OMUserVehicle__c");
                    //These tables are only snowf
                    //search.Add(sfnamespace + "OMAccount__c");
                    //search.Add(sfnamespace + "OMAccountAddress__c");
                    //search.Add(sfnamespace + "OMAccountProductFields__c");
                    //search.Add(sfnamespace + "OMAccountSalesForceFields__c");
                    //search.Add(sfnamespace + "OMAccountTerritoryFields__c");
                    //search.Add(sfnamespace + "OMAffiliation__c");
                    search.Add(sfnamespace + "OMGeographyAccount__c");
                    search.Add(sfnamespace + "OMGeographyHierarchy__c");                    
                    search.Add(sfnamespace + "OMProductHierarchy__c");
                    search.Add(sfnamespace + "OMRegionHierarchy__c");
                    break;
                case "INPUT":
                    //search.Add(sfnamespace + "OMScenarioAccountExclusion__c");
                    //search.Add(sfnamespace + "OMScenarioAccountExplicit__c");
                    search.Add(sfnamespace + "OMScenarioGeographyTerritory__c");
                    search.Add(sfnamespace + "OMScenarioProductExclusion__c");
                    search.Add(sfnamespace + "OMScenarioProductExplicit__c");
                    search.Add(sfnamespace + "OMScenarioProductSalesForce__c");
                    search.Add(sfnamespace + "OMScenarioSalesForceHierarchy__c");
                    search.Add(sfnamespace + "OMScenarioRule__c");
                    search.Add(sfnamespace + "OMScenarioSalesForce__c");
                    search.Add(sfnamespace + "OMScenarioTerritoryHierarchy__c");
                    search.Add(sfnamespace + "OMScenarioTerritory__c");
                    search.Add(sfnamespace + "OMScenarioUserAssignment__c");
                    break;
                case "MAIN":
                    search.Add(sfnamespace + "OMRegion__c");
                    search.Add(sfnamespace + "OMGeography__c");
                    search.Add(sfnamespace + "OMSalesForce__c");
                    search.Add(sfnamespace + "OMTerritory__c");
                    search.Add(sfnamespace + "OMUser__c");
                    search.Add(sfnamespace + "OMProduct__c");
                    search.Add(sfnamespace + "OMScenario__c");
                    search.Add(sfnamespace + "OMRule__c");
                    search.Add(sfnamespace + "OMRuleVersion__c");
                    break;
                default:
                    break;
            }
       
            var retDic = kvp.Where(pair => search.Contains(pair.Value))
                     .ToDictionary(pair => pair.Key, pair => pair.Value);

            return retDic;
        }

        public Dictionary<string, string> filterByLevel(Dictionary<string, string> kvp, int level, string xnamespace = null)
        {
            string sfnamespace = xnamespace ?? _namespace;
            var search = new HashSet<string>();
            switch (level)
            {
                case 0:
                    //These are root tables
                    search.Add(sfnamespace + "OMRegion__c");
                    break;
                case 1:
                    search.Add(sfnamespace + "OMGeography__c");
                    search.Add(sfnamespace + "OMSalesForce__c");
                    search.Add(sfnamespace + "OMTerritory__c");
                    search.Add(sfnamespace + "OMUser__c");
                    search.Add(sfnamespace + "OMProduct__c");
                    search.Add(sfnamespace + "OMScenario__c");
                    search.Add(sfnamespace + "OMRule__c");
                    break;
                case 2:
                    search.Add(sfnamespace + "OMRuleVersion__c");
                    search.Add(sfnamespace + "OMGeographyTerritory__c");
                    search.Add(sfnamespace + "OMProductExclusion__c");
                    search.Add(sfnamespace + "OMProductExplicit__c");
                    search.Add(sfnamespace + "OMProductSalesForce__c");
                    search.Add(sfnamespace + "OMProductTerritory__c");
                    search.Add(sfnamespace + "OMSalesForceHierarchy__c");
                    search.Add(sfnamespace + "OMScenarioError__c");
                    search.Add(sfnamespace + "OMScenarioSummary__c");
                    search.Add(sfnamespace + "OMTerritoryHierarchy__c");
                    search.Add(sfnamespace + "OMTerritorySalesForce__c");
                    search.Add(sfnamespace + "OMUserAssignment__c");
                    search.Add(sfnamespace + "OMUserAddress__c");
                    search.Add(sfnamespace + "OMUserCommunication__c");
                    search.Add(sfnamespace + "OMUserContact__c");
                    search.Add(sfnamespace + "OMUserDependent__c");
                    search.Add(sfnamespace + "OMUserDriver__c");
                    search.Add(sfnamespace + "OMUserEducation__c");
                    search.Add(sfnamespace + "OMUserVehicle__c");
                    search.Add(sfnamespace + "OMGeographyAccount__c");
                    search.Add(sfnamespace + "OMGeographyHierarchy__c");
                    search.Add(sfnamespace + "OMProductHierarchy__c");
                    search.Add(sfnamespace + "OMRegionHierarchy__c");
                    search.Add(sfnamespace + "OMScenarioAccountExclusion__c");
                    search.Add(sfnamespace + "OMScenarioAccountExplicit__c");
                    search.Add(sfnamespace + "OMScenarioProductExclusion__c");
                    search.Add(sfnamespace + "OMScenarioProductExplicit__c");
                    search.Add(sfnamespace + "OMScenarioRule__c");
                    search.Add(sfnamespace + "OMScenarioSalesForce__c");
                    search.Add(sfnamespace + "OMScenarioTerritory__c");
                    search.Add(sfnamespace + "OMScenarioTerritoryHierarchy__c");
                    search.Add(sfnamespace + "OMScenarioGeographyTerritory__c");
                    search.Add(sfnamespace + "OMScenarioProductSalesForce__c");
                    search.Add(sfnamespace + "OMScenarioSalesForceHierarchy__c");
                    search.Add(sfnamespace + "OMScenarioUserAssignment__c");
                    break;
                default:
                    search.Add(sfnamespace + "OMUserDriverIncident__c");
                    break;
            }

            var retDic = kvp.Where(pair => search.Contains(pair.Value))
                        .ToDictionary(pair => pair.Key, pair => pair.Value);

            return retDic;
        }
        public string getTableCount(string strTableName)
        {
            Task<QueryResult<dynamic>> result = _client.QueryAsync<dynamic>(string.Format("SELECT Id FROM {0}", strTableName));
            //var result = await _client.QueryAsync(string.Format("SELECT Id FROM {0}", strTableName));
            //return result.Records.Count.ToString();
            result.Wait();
            return result.Result.Records.Count.ToString();
        }
        public string queryReturnCellOneRecord(string strQuery, string fieldToGet)
        {
            Task<QueryResult<dynamic>> result = _client.QueryAsync<dynamic>(strQuery);
            result.Wait();
            var lstReturn = result.Result.Records;
            var nodeValues = lstReturn[0];
            return nodeValues[fieldToGet].ToString();           
        }

        public List<object> queryReturnCollection(string strQuery)
        {
            Task<QueryResult<dynamic>> result = _client.QueryAsync<dynamic>(strQuery);
            result.Wait();
            var retList = result.Result.Records;
            return retList; 
        }

    }
}