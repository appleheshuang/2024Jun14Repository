using MainFramework;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMREngineTest;
using OMREngineTest.Utils;
using SmokeTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace OCESyncTest.Utils
{
    public class SnowFlakeBuildEntities
    {
        private RequestSender _send;
        private readonly ITestOutputHelper _output;
        private SalesForceDataCollector _collectSalesForce;
        private string _baseURL; private string _clientID; private string _userName; private string _passWord; private string _clientSecret;
        //public string tenantOceSyncconfigfile = "preprod-org.json"; 
        //public string environment = "preprod";
        public string tenantOceSyncconfigfile = "master-org.json"; public string environment = "master";
        public Product smoke_product; public User smoke_user; public UserAddress smoke_useraddress;
        public MainFramework.Salesforce salesforce; public ScenarioRule Rule;
        public string ProductId; public Scenario smoke_scenario;
        public string UserId; public string RegionId; public string TerritoryHierarchyId; public string UserAssignmentId; public string ProdTerritoryId; public User smoke_user2;
        public MainFramework.Region smoke_region;
        public ScenarioDefinition smoketest;
        public string ScenarioId; public string SalesforceId; public string TerritoryId;
        private string _sfNameSpace; private string _orgURL; private string _accessToken;
        private OptimizerApi _engineApi;
        private string _xApiKey;

        public SnowFlakeBuildEntities(ITestOutputHelper output)
        {
            _output = output;
            output.WriteLine("input received");
            TenantFixture _tenantData = new TenantFixture(tenantOceSyncconfigfile);

            _baseURL = _tenantData.Config.baseURL;
            _clientID = _tenantData.Config.clientID;
            _clientSecret = _tenantData.Config.clientSecret;
            _userName = _tenantData.Config.userName;
            _passWord = _tenantData.Config.password;
            _sfNameSpace = _tenantData.Config.sfnamespace + "__";
            _orgURL = _tenantData.Config.orgURL;
            _xApiKey = _tenantData.Config.sfApiKey;
            environment = _tenantData.Config.endpoint;

            _collectSalesForce = new SalesForceDataCollector(_baseURL, _clientID, _clientSecret, _userName, _passWord, _sfNameSpace);
            _engineApi = new OptimizerApi(_tenantData.Config);

            _send = new RequestSender();

        }


        public Dictionary<string, List<string>> Scenariocommit()
        {

            _accessToken = _collectSalesForce.GET_SalesforceToken();

            CommonFunction commonFunc = new CommonFunction();

            Dictionary<string, List<string>> objDictionary = new Dictionary<string, List<string>>();

            List<string> objProductList = new List<string>();
            List<string> objUserList = new List<string>();
            List<string> objRegionList = new List<string>();
            List<string> objScenarioList = new List<string>();
            List<string> objSalesforceList = new List<string>();
            List<string> objTeritoryList = new List<string>();
            List<string> objProdTeritoryList = new List<string>();
            List<string> objTeritoryHierarchyList = new List<string>();
            List<string> objUserAssignmentList = new List<string>();
            List<string> objUserAddressList = new List<string>();

            Random rnd = new Random();

            string RegionUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            smoke_region = new MainFramework.Region();
            smoke_region.Description__c = "Region_" + RegionUniqueIntegrationID;
            smoke_region.name = "Region_" + RegionUniqueIntegrationID;
            smoke_region.EffectiveDate__c = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            //smoke_region.EffectiveDate__c = "2018-01-01";
            smoke_region.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
            smoke_region.SOMRegionId__c = "Region_" + RegionUniqueIntegrationID;
            smoke_region.Status__c = "ACTV";
            smoke_region.UniqueIntegrationId__c = RegionUniqueIntegrationID.ToString();

            //Create region and post to SF
            ScenarioFunction sf = new ScenarioFunction(_accessToken, _orgURL, _sfNameSpace);
            RegionId = sf.Post_Scenario("OMRegion__c", smoke_region);

            objRegionList.Add(RegionId);
            objDictionary.Add("Region", objRegionList);

            var regionName = smoke_region.name;


            string ProductUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            smoke_product = new Product();
            smoke_product.Description__c = "Product_" + ProductUniqueIntegrationID;
            smoke_product.name = "Product_" + ProductUniqueIntegrationID;
            smoke_product.EffectiveDate__c = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            smoke_product.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
            smoke_product.CompetitorProduct__c = true;
            smoke_product.UniqueIntegrationId__c = ProductUniqueIntegrationID.ToString();
            //_output.WriteLine("Unique intgration id " + smoke_product.UniqueIntegrationId__c);
            smoke_product.SOMProductId__c = "Product_" + ProductUniqueIntegrationID;
            smoke_product.Status__c = "ACTV";
            smoke_product.Type__c = "Brand";
            smoke_product.OMRegionId__c = RegionId;


            //Create user and post to SF
            // ScenarioFunction sf = new ScenarioFunction(_accessToken, _orgURL, _sfNameSpace);
            ProductId = sf.Post_Scenario("OMProduct__c", smoke_product);

            objProductList.Add(ProductId);
            objDictionary.Add("Product", objProductList);

            Dictionary<string, string> objDictUser = new Dictionary<string, string>();
            for (int i = 1; i <= 4; i++)
            {

                string UserUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
                smoke_user = new User();
                smoke_user.FirstName__c = "User";
                smoke_user.LastName__c = UserUniqueIntegrationID.ToString();

                if (i == 4)
                {
                    smoke_user.EffectiveDate__c = DateTime.Now.AddDays(+1).ToString("yyyy-MM-dd");
                    smoke_user.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");

                }
                else
                {
                    smoke_user.EffectiveDate__c = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                    smoke_user.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");

                }

                string strUserUniqueIntegrationId = "User_" + UserUniqueIntegrationID;
                smoke_user.UniqueIntegrationId__c = strUserUniqueIntegrationId;
                _output.WriteLine("Unique intgration id " + smoke_user.UniqueIntegrationId__c);
                string strUserId = "User_" + UserUniqueIntegrationID;
                smoke_user.SOMUserId__c = strUserId;
                smoke_user.Status__c = "ACTV";
                smoke_user.Email__c = "User" + UserUniqueIntegrationID + "@gmail.com";
                smoke_user.Type__c = "ADMN";
                smoke_user.EmailEncodingKey__c = "ISO-8859-1";
                smoke_user.LanguageLocaleKey__c = "en_US";
                smoke_user.LocaleSidKey__c = "en_US";
                smoke_user.TimeZoneSidKey__c = "America/Los_Angeles";
                objDictUser.Add(strUserUniqueIntegrationId, strUserId);

                objDictUser.First();

                UserId = sf.Post_Scenario("OMUser__c", smoke_user);
                objUserList.Add(UserId);

            }
            objDictionary.Add("User", objUserList);
            //Pass allobjDictTerritory Keys to List 
            List<string> objDictUserKeys = new List<string>(objDictUser.Keys);

            for (int l = 1; l <= 2; l++)
            {
                //New record in USERAddress table
                string UserAddressUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
                smoke_useraddress = new UserAddress();
                smoke_useraddress.Country__c = "US";
                smoke_useraddress.State__c = "CA";
                smoke_useraddress.EffectiveDate__c = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                smoke_useraddress.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
                string strUserAddressIntegrationId = "User_" + UserAddressUniqueIntegrationID;
                smoke_useraddress.UniqueIntegrationId__c = strUserAddressIntegrationId;
                if (l == 2)
                {
                    //smoke_useraddress.OMUserId__c = UserId;
                    smoke_useraddress.OMUserId__c = objUserList[3];
                }
                else
                {
                    //smoke_useraddress.OMUserId__c = UserId;
                    smoke_useraddress.OMUserId__c = objUserList[0];
                }
                smoke_useraddress.Status__c = "ACTV";
                smoke_useraddress.Type__c = "HOME";
                smoke_useraddress.City__c = "Norwich";

                string UserAddressId = sf.Post_Scenario("OMUserAddress__c", smoke_useraddress);
                objUserAddressList.Add(UserAddressId);
            }
            objDictionary.Add("UserAddress", objUserAddressList);



            string ScenarioUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            string strQuery = "select Id from " + _sfNameSpace + "OMRegion__c where name='" + regionName + "'";
            _output.WriteLine("query is" + strQuery);
            string region_id = _collectSalesForce.GetValue(_accessToken, strQuery, "Id");

            //Prepare scenario
            smoke_scenario = new Scenario();
            smoke_scenario.Description__c = "Scenario_" + ScenarioUniqueIntegrationID;
            smoke_scenario.Name = "Scenario_" + ScenarioUniqueIntegrationID;
            smoke_scenario.EffectiveDate__c = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            smoke_scenario.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
            smoke_scenario.OMRegionId__c = region_id;
            smoke_scenario.ScenarioStatus__c = "PENDG";
            smoke_scenario.UniqueIntegrationId__c = ScenarioUniqueIntegrationID.ToString();
            _output.WriteLine("scenario integration id " + smoke_scenario.UniqueIntegrationId__c);

            ScenarioId = sf.Post_Scenario("OMScenario__c", smoke_scenario);

            objScenarioList.Add(ScenarioId);
            objDictionary.Add("Scenario", objScenarioList);

            //SalesForce Data
            string SalesforceUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            salesforce = new MainFramework.Salesforce();
            salesforce.Action__c = "ADD";
            salesforce.OMScenarioId__c = ScenarioId;
            salesforce.UniqueIntegrationId__c = SalesforceUniqueIntegrationID.ToString();
            salesforce.SOMRegionId__c = smoke_region.SOMRegionId__c;
            salesforce.Type__c = "KOL";
            salesforce.name = "Salesforce_" + SalesforceUniqueIntegrationID;
            salesforce.RegionUniqueIntegrationId__c = smoke_region.UniqueIntegrationId__c;

            SalesforceId = sf.Post_Scenario("OMScenarioSalesForce__c", salesforce);

            objSalesforceList.Add(SalesforceId);
            objDictionary.Add("Salesforce", objSalesforceList);

            Dictionary<string, string> objDictTerritory = new Dictionary<string, string>();
            //Territory Data
            for (int k = 1; k <= 4; k++)
            {

                string TerritoryUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
                Territory territory = new Territory();
                territory.Action__c = "ADD";
                territory.OMScenarioId__c = ScenarioId;
                string strUniqueIntegrationId = "Territory_" + TerritoryUniqueIntegrationID;
                territory.SalesForceUniqueIntegrationId__c = salesforce.UniqueIntegrationId__c;
                territory.name = "Territory_" + TerritoryUniqueIntegrationID;
                territory.UniqueIntegrationId__c = strUniqueIntegrationId;
                territory.SOMRegionId__c = smoke_region.SOMRegionId__c;
                territory.Type__c = "TERR";
                string strTerritoryId = "Territory_" + TerritoryUniqueIntegrationID;
                territory.SOMTerritoryId__c = strTerritoryId;
                objDictTerritory.Add(strUniqueIntegrationId, strTerritoryId);

                objDictTerritory.First();
                //Build Territory and Post to SF
                TerritoryId = sf.Post_Scenario("OMScenarioTerritory__c", territory);

                objTeritoryList.Add(TerritoryId);

            }

            objDictionary.Add("Territory", objTeritoryList);
            //Pass allobjDictTerritory Keys to List 
            List<string> objDictKeys = new List<string>(objDictTerritory.Keys);
            // List<string> objDictKeys = objDictTerritory.Keys.ToList();


            //Adding territory hierarchy
            string TerritoryHierarchyUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
            TerritoryHierarchy territoryHierarchy = new TerritoryHierarchy();
            territoryHierarchy.Action__c = "ADD";
            /*if (i == 2)
            {
                territoryHierarchy.ChildTerritoryUniqueIntegrationId__c = objDictKeys[0];
                territoryHierarchy.ParentTerritoryUniqueIntegrationId__c = objDictKeys[2];
                territoryHierarchy.SOMChildTerritoryId__c = objDictTerritory[objDictKeys[0]];
                territoryHierarchy.SOMParentTerritoryId__c = objDictTerritory[objDictKeys[2]];
                territoryHierarchy.RefChildTerritoryName__c = objDictKeys[0];
                territoryHierarchy.RefParentTerritoryName__c = objDictKeys[2];
            }
            else
            {*/
            territoryHierarchy.ChildTerritoryUniqueIntegrationId__c = objDictKeys[0];
            territoryHierarchy.ParentTerritoryUniqueIntegrationId__c = objDictKeys[1];
            territoryHierarchy.SOMChildTerritoryId__c = objDictTerritory[objDictKeys[0]];
            territoryHierarchy.SOMParentTerritoryId__c = objDictTerritory[objDictKeys[1]];
            territoryHierarchy.RefChildTerritoryName__c = objDictKeys[0];
            territoryHierarchy.RefParentTerritoryName__c = objDictKeys[1];
            // }

            territoryHierarchy.UniqueIntegrationId__c = TerritoryHierarchyUniqueIntegrationID;
            territoryHierarchy.OMScenarioId__c = ScenarioId;


            //Build TerritoryHierarchy and Post to SF
            TerritoryHierarchyId = sf.Post_Scenario("OMScenarioTerritoryHierarchy__c", territoryHierarchy);
            objTeritoryHierarchyList.Add(TerritoryHierarchyId);
            //}
            objDictionary.Add("TerritoryHierarchy", objTeritoryHierarchyList);


            //Adding product to territory for OMProductTerritory
            string ProductTerritoryUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
            ProductTerritory productterritory = new ProductTerritory();
            productterritory.Action__c = "ADD";
            productterritory.SOMProductId__c = smoke_product.SOMProductId__c;
            productterritory.SOMTerritoryId__c = objDictTerritory[objDictKeys[0]];
            productterritory.ProductAlignmentType__c = "SMOD";
            productterritory.ProductUniqueIntegrationId__c = smoke_product.UniqueIntegrationId__c;
            productterritory.TerritoryUniqueIntegrationId__c = objDictKeys[0];
            productterritory.UniqueIntegrationId__c = ProductTerritoryUniqueIntegrationID.ToString();
            productterritory.OMScenarioId__c = ScenarioId;
            productterritory.RefProductName__c = smoke_product.name;
            productterritory.RefTerritoryName__c = objDictKeys[0];

            //Build Territory and Post to SF
            ProdTerritoryId = sf.Post_Scenario("OMScenarioProductExplicit__c", productterritory);

            objProdTeritoryList.Add(ProdTerritoryId);
            objDictionary.Add("ProductTerritory", objProdTeritoryList);


            //Assigning user to territory
            for (int m = 1; m <= 3; m++)
            {
                string UserAssignmentUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
                UserAssignment userassignment = new UserAssignment();
                userassignment.Action__c = "ADD";
                userassignment.OMScenarioId__c = ScenarioId;
                userassignment.RefUserType__c = "ADMN";
                userassignment.RefTerritoryType__c = "TERR";
                userassignment.UniqueIntegrationId__c = UserAssignmentUniqueIntegrationID;
                userassignment.AssignmentType__c = "ADMN";

                if (m == 3)
                {
                    userassignment.SOMTerritoryId__c = objDictTerritory[objDictKeys[2]];
                    userassignment.RefTerritoryName__c = objDictKeys[2];
                    userassignment.TerritoryUniqueIntegrationId__c = objDictKeys[2];
                    userassignment.UserUniqueIntegrationId__c = objDictUserKeys[2];
                    userassignment.SOMUserId__c = objDictUser[objDictUserKeys[2]];
                }

                else if (m == 2)
                {
                    userassignment.SOMTerritoryId__c = objDictTerritory[objDictKeys[1]];
                    userassignment.RefTerritoryName__c = objDictKeys[1];
                    userassignment.TerritoryUniqueIntegrationId__c = objDictKeys[1];
                    userassignment.UserUniqueIntegrationId__c = objDictUserKeys[1];
                    userassignment.SOMUserId__c = objDictUser[objDictUserKeys[1]];
                }
                else if (m == 1)
                {
                    userassignment.SOMTerritoryId__c = objDictTerritory[objDictKeys[0]];
                    userassignment.RefTerritoryName__c = objDictKeys[0];
                    userassignment.TerritoryUniqueIntegrationId__c = objDictKeys[0];
                    userassignment.UserUniqueIntegrationId__c = objDictUserKeys[0];
                    userassignment.SOMUserId__c = objDictUser[objDictUserKeys[0]];

                }
                //Build Territory and Post to SF
                UserAssignmentId = sf.Post_Scenario("OMScenarioUserAssignment__c", userassignment);
                objUserAssignmentList.Add(UserAssignmentId);
            }
            objDictionary.Add("UserAssignment", objUserAssignmentList);


            foreach (KeyValuePair<string, List<string>> objKey in objDictionary)
            {
                foreach (string objvalue in objKey.Value)
                {

                    Console.WriteLine(objKey.Key, objvalue);
                    _output.WriteLine("Key={0},Value={1}", objKey.Key, objvalue);

                }

            }

            JObject token_obj = _collectSalesForce.GET_EngineToken();
            _engineApi.SetToken(token_obj["token"].ToString(), token_obj["refresh"].ToString(), token_obj["lexiApiKey"].ToString(), token_obj["endpoint"].ToString());


            //create scenario body
            var scenarioRequestBody = new ScenarioApiRequestBody(ScenarioId);
            // var body = JsonConvert.SerializeObject(scenarioRequestBody);

            // _output.WriteLine("scenarioRequestBody" + body);

            //run scenario
            var response = _engineApi.RequestStatus("scenarioCommit", "POST", scenarioRequestBody);



            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // return objDictionary;
            return new Dictionary<string, List<string>>()
            {
                {"Product", objProductList },
                {"User", objUserList },
                {"UserAddress", objUserAddressList },
                {"Region", objRegionList },
                {"Scenario", objScenarioList },
                {"Salesforce", objSalesforceList },
                {"Territory", objTeritoryList },
                {"ProductTerritory", objProdTeritoryList },
                {"TerritoryHierarchy", objTeritoryHierarchyList },
                {"UserAssignment", objUserAssignmentList},

        };
        }


        public Dictionary<string, List<string>> Scenariorule()
        {

            _accessToken = _collectSalesForce.GET_SalesforceToken();

            CommonFunction commonFunc = new CommonFunction();

            Dictionary<string, List<string>> objDictionary = new Dictionary<string, List<string>>();


            List<string> objRegionList = new List<string>();
            List<string> objScenarioList = new List<string>();
            List<string> objSalesforceList = new List<string>();
            List<string> objTeritoryList = new List<string>();
            List<string> objRuleList = new List<string>();


            Random rnd = new Random();


            //Create user and post to SF
            ScenarioFunction sf = new ScenarioFunction(_accessToken, _orgURL, _sfNameSpace);


            string RegionUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            smoke_region = new MainFramework.Region();
            smoke_region.Description__c = "Region_" + RegionUniqueIntegrationID;
            smoke_region.name = "Region_" + RegionUniqueIntegrationID;
            smoke_region.EffectiveDate__c = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            //smoke_region.EffectiveDate__c = "2018-01-01";
            smoke_region.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
            smoke_region.SOMRegionId__c = "Region_" + RegionUniqueIntegrationID;
            smoke_region.Status__c = "ACTV";
            smoke_region.UniqueIntegrationId__c = RegionUniqueIntegrationID.ToString();

            RegionId = sf.Post_Scenario("OMRegion__c", smoke_region);

            objRegionList.Add(RegionId);
            objDictionary.Add("Region", objRegionList);

            var regionName = smoke_region.name;

            string ScenarioUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            string strQuery = "select Id from " + _sfNameSpace + "OMRegion__c where name='" + regionName + "'";
            _output.WriteLine("query is" + strQuery);
            string region_id = _collectSalesForce.GetValue(_accessToken, strQuery, "Id");

            //Prepare scenario
            smoke_scenario = new Scenario();
            smoke_scenario.Description__c = "Scenario_" + ScenarioUniqueIntegrationID;
            smoke_scenario.Name = "Scenario_" + ScenarioUniqueIntegrationID;
            smoke_scenario.EffectiveDate__c = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            smoke_scenario.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
            smoke_scenario.OMRegionId__c = region_id;
            smoke_scenario.ScenarioStatus__c = "PENDG";
            smoke_scenario.UniqueIntegrationId__c = ScenarioUniqueIntegrationID.ToString();
            _output.WriteLine("scenario integration id " + smoke_scenario.UniqueIntegrationId__c);

            ScenarioId = sf.Post_Scenario("OMScenario__c", smoke_scenario);

            objScenarioList.Add(ScenarioId);
            objDictionary.Add("Scenario", objScenarioList);

            //SalesForce Data
            string SalesforceUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            salesforce = new MainFramework.Salesforce();
            salesforce.Action__c = "ADD";
            salesforce.OMScenarioId__c = ScenarioId;
            salesforce.UniqueIntegrationId__c = SalesforceUniqueIntegrationID.ToString();
            salesforce.SOMRegionId__c = smoke_region.SOMRegionId__c;
            salesforce.Type__c = "KOL";
            salesforce.name = "Salesforce_" + SalesforceUniqueIntegrationID;
            salesforce.RegionUniqueIntegrationId__c = smoke_region.UniqueIntegrationId__c;

            SalesforceId = sf.Post_Scenario("OMScenarioSalesForce__c", salesforce);

            objSalesforceList.Add(SalesforceId);
            objDictionary.Add("Salesforce", objSalesforceList);

            Dictionary<string, string> objDictTerritory = new Dictionary<string, string>();
            //Territory Data
            for (int k = 1; k <= 2; k++)
            {

                string TerritoryUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
                Territory territory = new Territory();
                territory.Action__c = "ADD";
                territory.OMScenarioId__c = ScenarioId;
                string strUniqueIntegrationId = "Territory_" + TerritoryUniqueIntegrationID;
                territory.SalesForceUniqueIntegrationId__c = salesforce.UniqueIntegrationId__c;
                territory.name = "Territory_" + TerritoryUniqueIntegrationID;
                territory.UniqueIntegrationId__c = strUniqueIntegrationId;
                territory.SOMRegionId__c = smoke_region.SOMRegionId__c;
                territory.Type__c = "TERR";
                string strTerritoryId = "Territory_" + TerritoryUniqueIntegrationID;
                territory.SOMTerritoryId__c = strTerritoryId;
                objDictTerritory.Add(strUniqueIntegrationId, strTerritoryId);

                objDictTerritory.First();
                //Build Territory and Post to SF
                TerritoryId = sf.Post_Scenario("OMScenarioTerritory__c", territory);

                objTeritoryList.Add(TerritoryId);

            }

            objDictionary.Add("Territory", objTeritoryList);
            //Pass allobjDictTerritory Keys to List 
            List<string> objDictKeys = new List<string>(objDictTerritory.Keys);
            // List<string> objDictKeys = objDictTerritory.Keys.ToList();


            //Adding Rule
            string RuleUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
            Rule = new ScenarioRule();
            Rule.Action__c = "ADD";
            Rule.OMScenarioId__c = ScenarioId;
            Rule.UniqueIntegrationId__c = RuleUniqueIntegrationID;
            Rule.SalesForceUniqueIntegrationId__c = salesforce.UniqueIntegrationId__c;
            Rule.Name = "Rule_" + RuleUniqueIntegrationID;
            string ruledata = "{\"geography\":false,\"explicit\":false,\"filter\":{\"type\":\"In\",\"table\":\"OMAccount\",\"column\":\"AccountType\",\"value\":\"MP\"}}";
            Rule.RuleData__c = ruledata;

            string RuleId = sf.Post_Scenario("OMScenarioRule__c", Rule);
            objRuleList.Add(RuleId);
            objDictionary.Add("Rule", objRuleList);


            foreach (KeyValuePair<string, List<string>> objKey in objDictionary)
            {
                foreach (string objvalue in objKey.Value)
                {

                    Console.WriteLine(objKey.Key, objvalue);
                    _output.WriteLine("Key={0},Value={1}", objKey.Key, objvalue);

                }

            }

            JObject token_obj = _collectSalesForce.GET_EngineToken();
            _engineApi.SetToken(token_obj["token"].ToString(), token_obj["refresh"].ToString(), token_obj["lexiApiKey"].ToString(), token_obj["endpoint"].ToString());

            //create scenario body
            var scenarioRequestBody = new ScenarioApiRequestBody(ScenarioId);

            //run scenario
            var response = _engineApi.RequestStatus("scenarioCommit", "POST", scenarioRequestBody);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return new Dictionary<string, List<string>>()
            {

                {"Region", objRegionList },
                {"Scenario", objScenarioList },
                {"Salesforce", objSalesforceList },
                {"Territory", objTeritoryList },
                {"Rule", objRuleList },


        };
        }

        public string Fun_OCE_AccountCreation()
        {

            List<string> objAccountId = new List<string>();
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            EnvFixture _api = new EnvFixture("envConfig-master.json");
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            CommonFunction commFunction = new CommonFunction();
            Random rnd = new Random();

            Dictionary<string, object> objAccount = new Dictionary<string, object>();
            objAccount.Add("FirstName", "Automation");
            objAccount.Add("LastName", "Testing " + commFunction.GenerateRndNumber(3, rnd));
            objAccount.Add("OCE__UniqueIntegrationID__c", commFunction.GenerateRndNumber(12, rnd));
            objAccount.Add("OCE__Status__c", "Active");
            objAccount.Add("OCE__IndividualTypes__c", "MP");
            string strAccountID = sf.Post_Scenario("Account", objAccount);
            _output.WriteLine(strAccountID);

            return strAccountID;
        }

    }
}
