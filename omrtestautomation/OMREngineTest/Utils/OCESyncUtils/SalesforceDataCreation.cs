using MainFramework;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using Newtonsoft.Json.Linq;
using OCESyncTest.Utils;
using OMREngineTest;
using OMREngineTest.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace OCESyncTest.Tests
{
    public class SalesforceDataCreation
    {
        private RequestSender _send;
        private CommonFunction commFunction;
        private readonly ITestOutputHelper _output;
        private SalesForceDataCollector _collectSalesForce; private SnowflakeDBData _sfdbdata;
        private string _baseURL; private string _clientID; private string _userName; private string _passWord; private string _clientSecret;
        private OptimizerApi _engineApi;
        private string _xApiKey;
        private string _snowConnection;
        private string _sfDBName;
        /*public string tenantOceSyncconfigfile = "preprod-org.json";
        public string environment = "preprod";*/
        public string tenantOceSyncconfigfile = "master-org.json";
        public string environment = "master";
        private MainFramework.Salesforce salesforce;
        private PULLAccountTest objPULLAccount;

        public SalesforceDataCreation(ITestOutputHelper output)
        {
            _output = output;
            output.WriteLine("input received");
            TenantFixture _tenantData = new TenantFixture(tenantOceSyncconfigfile);

            _sfdbdata = new SnowflakeDBData(_output);

            _baseURL = _tenantData.Config.baseURL;
            _clientID = _tenantData.Config.clientID;
            _clientSecret = _tenantData.Config.clientSecret;
            _userName = _tenantData.Config.userName;
            _passWord = _tenantData.Config.password;
            _snowConnection = _tenantData.Config.snowConnection;
            _xApiKey = _tenantData.Config.sfApiKey;
            environment = _tenantData.Config.endpoint;
            _sfDBName = GetDBName();
            _collectSalesForce = new SalesForceDataCollector(_baseURL, _clientID, _clientSecret, _userName, _passWord);
            _send = new RequestSender();
            _engineApi = new OptimizerApi(_tenantData.Config);
            commFunction = new CommonFunction();
            objPULLAccount = new PULLAccountTest(_output);

        }

        public string GetDBName()
        {
            String[] myString = _snowConnection.Split(';');
            Dictionary<string, string> dbDetailsDict = new Dictionary<string, string>();
            for (int i = 0; i < myString.Count(); i++)
            {
                String[] connectionDetails = myString[i].Split('=');
                dbDetailsDict.Add(connectionDetails[0], connectionDetails[1]);
            }
            return dbDetailsDict["DB"];
        }


        public Tuple<string, string> Fun_RegionCreation()
        {
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            Random rnd = new Random();
            TenantFixture _tenantData = new TenantFixture(tenantOceSyncconfigfile);

            string sfNameSpace = _tenantData.Config.sfnamespace + "__";
            CommonFunction commonFunc = new CommonFunction();
            ScenarioFunction objsf = new ScenarioFunction(strAccessToken, _baseURL, "");
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, sfNameSpace);

            string RegionUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            Dictionary<string, object> smoke_region = new Dictionary<string, object>();

            string strRegionId = "TestRegion_" + commonFunc.GenerateRndNumber(3, rnd);
            smoke_region.Add("Name", "TestRegion_" + commonFunc.GenerateRndNumber(3, rnd));
            smoke_region.Add("iq_sj_omr_01__Description__c", "TestRegionDesc_" + commonFunc.GenerateRndNumber(3, rnd));
            smoke_region.Add("iq_sj_omr_01__EffectiveDate__c", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));
            smoke_region.Add("iq_sj_omr_01__EndDate__c", DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd"));
            smoke_region.Add("iq_sj_omr_01__SOMRegionId__c", strRegionId);
            smoke_region.Add("iq_sj_omr_01__Status__c", "ACTV");
            smoke_region.Add("iq_sj_omr_01__UniqueIntegrationId__c", commonFunc.GenerateRndNumber(13, rnd));
            smoke_region.Add("iq_sj_omr_01__Country__c", "US");

            string RegionId = objsf.Post_Scenario("iq_sj_omr_01__OMRegion__c", smoke_region);
            _output.WriteLine("Region reated is : " + RegionId);

            //Scenario Creation 
            string ScenarioUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);
            Scenario smoke_scenario = new Scenario();
            smoke_scenario.Description__c = "TestScenario " + ScenarioUniqueIntegrationID;
            smoke_scenario.Name = "TestingScenario " + ScenarioUniqueIntegrationID;
            smoke_scenario.EffectiveDate__c = DateTime.Now.ToString("yyyy-MM-dd");
            smoke_scenario.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
            smoke_scenario.OMRegionId__c = RegionId;
            smoke_scenario.ScenarioStatus__c = "PENDG";
            smoke_scenario.UniqueIntegrationId__c = ScenarioUniqueIntegrationID.ToString();
            _output.WriteLine("scenario integration id " + smoke_scenario.UniqueIntegrationId__c);

            //Build Scenario and post to SF            
            string ScenarioId = sf.Post_Scenario("OMScenario__c", smoke_scenario);
            string SalesforceUniqueIntegrationID = commonFunc.GenerateRndNumber(3, rnd);

            salesforce = new MainFramework.Salesforce();
            salesforce.Action__c = "ADD";
            salesforce.OMScenarioId__c = ScenarioId;
            salesforce.UniqueIntegrationId__c = SalesforceUniqueIntegrationID.ToString();
            // salesforce.SOMRegionId__c = smoke_region.SOMRegionId__c;
            salesforce.SOMRegionId__c = smoke_region["iq_sj_omr_01__SOMRegionId__c"].ToString();
            salesforce.Type__c = "KOL";
            salesforce.name = "Salesforce " + SalesforceUniqueIntegrationID;
            // salesforce.RegionUniqueIntegrationId__c = smoke_region.UniqueIntegrationId__c;
            salesforce.RegionUniqueIntegrationId__c = smoke_region["iq_sj_omr_01__UniqueIntegrationId__c"].ToString();
            //Build SalesForce and Post to SF
            string SalesforceId = sf.Post_Scenario("OMScenarioSalesForce__c", salesforce);

            JObject token_obj = _collectSalesForce.GET_EngineToken();

            //create scenario body
            var scenarioRequestBody = new ScenarioApiRequestBody(ScenarioId);

            //run scenario
            _engineApi.SetToken(token_obj["token"].ToString(), token_obj["refresh"].ToString(), token_obj["lexiApiKey"].ToString(), token_obj["endpoint"].ToString());
            var response = _engineApi.RequestStatus("scenariocommit", "POST", scenarioRequestBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            //return strRegionId;
            return new Tuple<string, string>(strRegionId, smoke_region["iq_sj_omr_01__Country__c"].ToString());


        }







        //  [Fact]
        public Dictionary<string, object> Fun_ScenarioRuleCreation()

        {
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            Random rnd = new Random();
            TenantFixture _tenantData = new TenantFixture(tenantOceSyncconfigfile);
            string sfNameSpace = _tenantData.Config.sfnamespace + "__";
            ScenarioFunction objsf = new ScenarioFunction(strAccessToken, _baseURL, "");
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, sfNameSpace);
            string RegionUniqueIntegrationID = commFunction.GenerateRndNumber(3, rnd);
            Dictionary<string, object> objcenarioRule = new Dictionary<string, object>();
            Dictionary<string, object> smoke_region = new Dictionary<string, object>();

            /*
             string strAccountId = null, sfAccountUniqueIntegrationId = null;
             if (strAccount.Equals("Y"))
            {
                strAccountId = Fun_OCE_AccountCreation();

                string sfAccountCreationQuery = "SELECT Id, Name, OCE__UniqueIntegrationID__c FROM Account where Id = '" + strAccountId + "'";
                sfAccountUniqueIntegrationId = _collectSalesForce.GetValue(strAccessToken, sfAccountCreationQuery, "OCE__UniqueIntegrationID__c");
                objPULLAccount.GetPullJob("", false);
                Thread.Sleep(150000);
            }*/

            //Region
            string strRegionId = "TestRegion_" + commFunction.GenerateRndNumber(3, rnd);
            smoke_region.Add("Name", "TestRegion_" + commFunction.GenerateRndNumber(3, rnd));
            smoke_region.Add("iq_sj_omr_01__Description__c", "TestRegionDesc_" + commFunction.GenerateRndNumber(3, rnd));
            smoke_region.Add("iq_sj_omr_01__EffectiveDate__c", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));
            smoke_region.Add("iq_sj_omr_01__EndDate__c", DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd"));
            smoke_region.Add("iq_sj_omr_01__SOMRegionId__c", strRegionId);
            smoke_region.Add("iq_sj_omr_01__Status__c", "ACTV");
            smoke_region.Add("iq_sj_omr_01__UniqueIntegrationId__c", commFunction.GenerateRndNumber(13, rnd));
            //smoke_region.Add("iq_sj_omr_01__Country__c", "US");
            string RegionId = objsf.Post_Scenario("iq_sj_omr_01__OMRegion__c", smoke_region);
            _output.WriteLine("Region reated is : " + RegionId);
            objcenarioRule.Add("Region", RegionId);

            //Scenario Creation 
            string ScenarioUniqueIntegrationID = commFunction.GenerateRndNumber(3, rnd);
            Scenario smoke_scenario = new Scenario();
            smoke_scenario.Description__c = "TestScenario " + ScenarioUniqueIntegrationID;
            smoke_scenario.Name = "TestingScenario " + ScenarioUniqueIntegrationID;
            smoke_scenario.EffectiveDate__c = DateTime.Now.ToString("yyyy-MM-dd");
            smoke_scenario.EndDate__c = DateTime.Now.AddYears(+1).ToString("yyyy-MM-dd");
            smoke_scenario.OMRegionId__c = RegionId;
            smoke_scenario.ScenarioStatus__c = "PENDG";
            smoke_scenario.UniqueIntegrationId__c = ScenarioUniqueIntegrationID.ToString();
            _output.WriteLine("scenario integration id " + smoke_scenario.UniqueIntegrationId__c);
            //Build Scenario and post to SF            
            string ScenarioId = sf.Post_Scenario("OMScenario__c", smoke_scenario);
            objcenarioRule.Add("ScenarioId", ScenarioId);
            string SalesforceUniqueIntegrationID = commFunction.GenerateRndNumber(3, rnd);
            //SalesForce
            salesforce = new MainFramework.Salesforce();
            salesforce.Action__c = "ADD";
            salesforce.OMScenarioId__c = ScenarioId;
            salesforce.UniqueIntegrationId__c = SalesforceUniqueIntegrationID.ToString();
            // salesforce.SOMRegionId__c = smoke_region.SOMRegionId__c;
            salesforce.SOMRegionId__c = smoke_region["iq_sj_omr_01__SOMRegionId__c"].ToString();
            salesforce.Type__c = "KOL";
            salesforce.name = "Salesforce " + SalesforceUniqueIntegrationID;
            // salesforce.RegionUniqueIntegrationId__c = smoke_region.UniqueIntegrationId__c;
            salesforce.RegionUniqueIntegrationId__c = smoke_region["iq_sj_omr_01__UniqueIntegrationId__c"].ToString();
            //Build SalesForce and Post to SF
            string SalesforceId = sf.Post_Scenario("OMScenarioSalesForce__c", salesforce);
            objcenarioRule.Add("Salesforce", SalesforceId);
            //Territory
            string TerritoryUniqueIntegrationID = commFunction.GenerateRndNumber(3, rnd);
            Territory territory = new Territory();
            territory.Action__c = "ADD";
            territory.OMScenarioId__c = ScenarioId;
            string strUniqueIntegrationId = "Territory_" + TerritoryUniqueIntegrationID;
            territory.SalesForceUniqueIntegrationId__c = salesforce.UniqueIntegrationId__c;
            territory.name = "Territory_" + TerritoryUniqueIntegrationID;
            territory.UniqueIntegrationId__c = strUniqueIntegrationId;
            territory.SOMRegionId__c = smoke_region["iq_sj_omr_01__SOMRegionId__c"].ToString();
            territory.Type__c = "TERR";
            string strTerritoryId = "Territory_" + TerritoryUniqueIntegrationID;
            territory.SOMTerritoryId__c = strTerritoryId;
            string TerritoryIds = sf.Post_Scenario("OMScenarioTerritory__c", territory);
            objcenarioRule.Add("Territory", territory.name);

            //Rule
            string RuleUniqueIntegrationID = commFunction.GenerateRndNumber(3, rnd);
            Dictionary<string, object> objRule = new Dictionary<string, object>();
            objRule.Add("Action__c", "ADD");
            objRule.Add("OMScenarioId__c", ScenarioId);
            objRule.Add("UniqueIntegrationId__c", RuleUniqueIntegrationID);
            objRule.Add("SalesForceUniqueIntegrationId__c", salesforce.UniqueIntegrationId__c);
            objRule.Add("Name", "Rule_" + RuleUniqueIntegrationID);
            string ruledata = "{\"geography\":false,\"explicit\":false,\"filter\":{\"type\":\"In\",\"table\":\"OMAccount\",\"column\":\"AccountType\",\"value\":\"BC\"}}";
            objRule.Add("RuleData__c", ruledata);
            string RuleId = sf.Post_Scenario("OMScenarioRule__c", objRule);
            objcenarioRule.Add("Rule", RuleId);


            /* //Adding the Account to scenario
             if (strAccount.Equals("Y")) { 
                 objAccountTerritory = new AccountTerritory();
             objAccountTerritory.Action__c="ADD";
             objAccountTerritory.OMScenarioId__c = ScenarioId;
             objAccountTerritory.UniqueIntegrationId__c = commFunction.GenerateRndNumber(12, rnd);
             objAccountTerritory.TerritoryUniqueIntegrationId__c = territory.UniqueIntegrationId__c;
             objAccountTerritory.AccountUniqueIntegrationId__c = sfAccountUniqueIntegrationId;
             objAccountTerritory.OMAccountId__c = strAccountId;
             objAccountTerritory.SOMTerritoryId__c = territory.SOMTerritoryId__c;
             objAccountTerritory.RefTerritoryName__c = territory.name;
             string AccountTerritoryId = sf.Post_Scenario("OMScenarioAccountExplicit__c", objAccountTerritory);
             objcenarioRule.Add("Account", AccountTerritoryId);
             }*/


            JObject token_obj = _collectSalesForce.GET_EngineToken();
            //create scenario body
            var scenarioRequestBody = new ScenarioApiRequestBody(ScenarioId);
            //run scenario
            _engineApi.SetToken(token_obj["token"].ToString(), token_obj["refresh"].ToString(), token_obj["lexiApiKey"].ToString(), token_obj["endpoint"].ToString());
            var response = _engineApi.RequestStatus("scenariocommit", "POST", scenarioRequestBody);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return objcenarioRule;


        }



        public string Fun_OCE_AccountCreation(string strCountryCode = "US", string strMode = "Y")
        {

            List<string> objAccountId = new List<string>();
            //String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            string strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            Random rnd = new Random();
            Dictionary<string, object> objAccount = new Dictionary<string, object>();
            objAccount.Add("FirstName", "Automation");
            objAccount.Add("LastName", "Testing " + commFunction.GenerateRndNumber(3, rnd));
            objAccount.Add("OCE__UniqueIntegrationID__c", commFunction.GenerateRndNumber(12, rnd));
            objAccount.Add("OCE__ProfessionalTitle__c", "Medical Doctors");
            if (strMode.Equals("Y"))
            {
                objAccount.Add("OCE__CountryCode__c", strCountryCode);
            }
            objAccount.Add("OCE__Status__c", "Active");
            objAccount.Add("OCE__OneKeyID__c", "Testing OneKeyID " + commFunction.GenerateRndNumber(3, rnd));
            objAccount.Add("OCE__StructureType__c", "Department");
            objAccount.Add("OCE__IndividualTypes__c", "Business Contact");
            objAccount.Add("OCE__OrganizationTypes__c", "Department");
            objAccount.Add("OCE__Specialty__c", "Acupuncturist");
            objAccount.Add("OCE__Specialty2__c", "Emergency medical services");

            string strAccountID = sf.Post_Scenario("Account", objAccount);
            _output.WriteLine(strAccountID);

            return strAccountID;
        }



        public string Fun_OCE_AffiliationCreation(string strAccountIdTo, string AccountIdFrom)
        {

            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            Random rnd = new Random();
            Dictionary<object, object> objDictAffiliation = new Dictionary<object, object>();
            objDictAffiliation.Add("OCE__AffiliationType__c", "Soft");
            objDictAffiliation.Add("OCE__From__c", AccountIdFrom);
            objDictAffiliation.Add("OCE__To__c", strAccountIdTo);
            objDictAffiliation.Add("OCE__Roles__c", "GPO");
            objDictAffiliation.Add("OCE__UniqueIntegrationID__c", commFunction.GenerateRndNumber(12, rnd));
            objDictAffiliation.Add("OCE__IsPrimary__c", "false");
            objDictAffiliation.Add("OCE__Title__c", "Chairman");
            Thread.Sleep(2000);

            string strAffiliationId = sf.Post_Scenario("OCE__Affiliation__c", objDictAffiliation);
            return strAffiliationId;


        }
        public string OCE_AddressCreation()
        {

            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            Random rnd = new Random();
            AccountAddress objAddress = new AccountAddress();
            objAddress.Name = "Account Address " + commFunction.GenerateRndNumber(3, rnd);
            objAddress.OCE__AddressLine2__c = "AddressLine2" + commFunction.GenerateRndNumber(3, rnd);
            objAddress.OCE__AddressLine3__c = "AddressLine3" + commFunction.GenerateRndNumber(3, rnd);
            objAddress.OCE__AddressLine4__c = "AddressLine4" + commFunction.GenerateRndNumber(3, rnd);
            objAddress.OCE__City__c = "JERSEY CITY";
            objAddress.OCE__State__c = "TEST" + commFunction.GenerateRndNumber(2, rnd);
            objAddress.OCE__ZipCode__c = "760080";
            objAddress.OCE__Country__c = "US";
            objAddress.OCE__Inactive__c = "false";


            objAddress.OCE__CountryCode__c = "AU";

            objAddress.OCE__UniqueIntegrationID__c = commFunction.GenerateRndNumber(12, rnd);


            Thread.Sleep(2000);
            string strAddressId = sf.Post_Scenario("OCE__Address__c", objAddress);

            return strAddressId;

        }


        public JObject SalesForceRecod(string strQuery, string strToken)
        {
            string[] _engineTokens = new string[2];
            var strRequest = _baseURL + "/services/data/v46.0/query/?q=" + strQuery;
            Hashtable headers = new Hashtable();
            headers.Add("Authorization", strToken);
            Tuple<WebResponse, String> responsTuple = _send.RETURN_WebResponse(strRequest, "GET", headers, "NONE", "NONE");
            return JObject.Parse(responsTuple.Item2);
        }

        public string Fun_OCEAccountTerritory(string strAccountId)
        {
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            Random rnd = new Random();

            AccountTerritoryFields objAccountTerritory = new AccountTerritoryFields();
            objAccountTerritory.OCE__Territory__c = "Territory" + commFunction.GenerateRndNumber(3, rnd);
            objAccountTerritory.OCE__UniqueIntegrationID__c = commFunction.GenerateRndNumber(13, rnd);
            objAccountTerritory.OCE__Account__c = strAccountId;

            string strAccountTerritor = sf.Post_Scenario("OCE__AccountTerritoryFields__c", objAccountTerritory);

            return strAccountTerritor;
        }




        public string Fun_OCE_AccountAddress(string strAccountID, string strAddressId)
        {

            String strAccessToken = _collectSalesForce.GET_SalesforceToken();

            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");

            Random rnd = new Random();
            OCEAccountAddress objOCEAccountAddress = new OCEAccountAddress();
            objOCEAccountAddress.OCE__Account__c = strAccountID;
            objOCEAccountAddress.OCE__Address__c = strAddressId;
            objOCEAccountAddress.OCE__AddressType__c = "Business";
            objOCEAccountAddress.OCE__UniqueIntegrationID__c = commFunction.GenerateRndNumber(13, rnd);

            string strAccountAddress = sf.Post_Scenario("OCE__AccountAddress__c ", objOCEAccountAddress);
            _output.WriteLine(strAccountAddress);

            return strAccountAddress;
        }




        public void Fun_OCEAccountUpdate(string strAccountId)
        {
            Random rnd = new Random();
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");

            JObject token_obj = _collectSalesForce.GET_EngineToken();
            Dictionary<string, object> objUpdateAccount = new Dictionary<string, object>();
            objUpdateAccount.Add("OCE__OneKeyID__c", "Update OneKeyID" + commFunction.GenerateRndNumber(3, rnd));
            sf.Patch_Scenario("Account", objUpdateAccount, strAccountId);
        }
        public void Fun_OCEAffiliationIdUpdate(string strAffiliationId)
        {
            Random rnd = new Random();
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");

            JObject token_obj = _collectSalesForce.GET_EngineToken();
            Dictionary<object, object> objDict_Account_Affiliation = new Dictionary<object, object>();
            objDict_Account_Affiliation.Add("OCE__AffiliationType__c", "Hard");
            objDict_Account_Affiliation.Add("OCE__Roles__c", "IDN");
            sf.Patch_Scenario("OCE__Affiliation__c", objDict_Account_Affiliation, strAffiliationId);
        }
        public void Fun_OCEAddressUpdate(string strAddress)
        {
            Random rnd = new Random();
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");

            JObject token_obj = _collectSalesForce.GET_EngineToken();
            Dictionary<string, object> obj_Account_Address = new Dictionary<string, object>();
            obj_Account_Address.Add("Name", "Update Address " + commFunction.GenerateRndNumber(3, rnd));
            sf.Patch_Scenario("OCE__Address__c", obj_Account_Address, strAddress);
        }
        public void Fun_OCEAccountTerritoryUpdate(string strAccountTerritoryId)
        {

            Random rnd = new Random();
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");

            JObject token_obj = _collectSalesForce.GET_EngineToken();
            Dictionary<string, object> objAccountTerritory = new Dictionary<string, object>();
            objAccountTerritory.Add("OCE__Territory__c", "Territory" + commFunction.GenerateRndNumber(3, rnd));
            sf.Patch_Scenario("OCE__AccountTerritoryFields__c", objAccountTerritory, strAccountTerritoryId);


        }



        public bool Fun_OCEAccountDelete(string strAccountId)
        {
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");

            JObject token_obj = _collectSalesForce.GET_EngineToken();
            sf.Delete_Scenario("Account", strAccountId);

            Thread.Sleep(2000);

            string strSalesForceQuery = "SELECT Id,Type,OCE__RecordTypeDeveloperName__c,Name,FirstName,LastName,OCE__Status__c,OCE__UniqueIntegrationID__c,OCE__OneKeyID__c,OCE__ProfessionalTitle__c,OCE__StructureType__c,OCE__CountryCode__c,OCE__IndividualTypes__c,OCE__OrganizationTypes__c,OCE__Specialty__c,OCE__Specialty2__c,OCE__Specialty3__c,OCE__Specialty4__c,OCE__Specialty5__c FROM Account where Id = '" + strAccountId + "'";
            JObject objTotalRecord = SalesForceRecod(strSalesForceQuery, "Bearer " + strAccessToken);

            string strTotalSize = objTotalRecord["totalSize"].ToString();

            if (Int32.Parse(strTotalSize) == 0)
            {
                return true;
            }
            else
            {
                return false;

            }
        }

        public bool Fun_OCEAffiliationDelete(string strAffiliationId)
        {
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");

            JObject token_obj = _collectSalesForce.GET_EngineToken();

            sf.Delete_Scenario("OCE__Affiliation__c", strAffiliationId);
            Thread.Sleep(2000);

            string strSalesForceQuery = "SELECT Id, OCE__AffiliationType__c, OCE__From__c, OCE__To__c,OCE__IsActive__c,OCE__Roles__c,OCE__UniqueIntegrationID__c,OCE__IsPrimary__c,OCE__Title__c from OCE__Affiliation__c where Id = '" + strAffiliationId + "'";
            JObject objTotalRecord = SalesForceRecod(strSalesForceQuery, "Bearer " + strAccessToken);

            string strTotalSize = objTotalRecord["totalSize"].ToString();

            if (Int32.Parse(strTotalSize) == 0)
            {
                return true;
            }
            else
            {
                return false;

            }
        }

        public bool Fun_OCEAddressDelete(string strAddressId)
        {
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            JObject token_obj = _collectSalesForce.GET_EngineToken();
            sf.Delete_Scenario("OCE__Address__c", strAddressId);
            Thread.Sleep(2000);
            string strSalesForceQuery = "SELECT Id, Name, OCE__AddressLine2__c, OCE__AddressLine3__c, OCE__AddressLine4__c, OCE__City__c, OCE__State__c, OCE__ZipCode__c, OCE__Country__c, OCE__Inactive__c, OCE__UniqueIntegrationID__c, OCE__GeoLocation__Latitude__s, OCE__GeoLocation__Longitude__s FROM OCE__Address__c where Id = '" + strAddressId + "'";
            JObject objTotalRecord = SalesForceRecod(strSalesForceQuery, "Bearer " + strAccessToken);
            string strTotalSize = objTotalRecord["totalSize"].ToString();
            if (Int32.Parse(strTotalSize) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Fun_OCEAccountTerritoryDelete(string strAccountTerritoryId)
        {
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            JObject token_obj = _collectSalesForce.GET_EngineToken();
            sf.Delete_Scenario("OCE__AccountTerritoryFields__c", strAccountTerritoryId);
            Thread.Sleep(2000);
            string strSalesForceQuery = "SELECT Id, OCE__Territory__c, OCE__UniqueIntegrationID__c, OCE__Account__c, CreatedDate, LastModifiedDate FROM OCE__AccountTerritoryFields__c where Id = '" + strAccountTerritoryId + "'";
            JObject objTotalRecord = SalesForceRecod(strSalesForceQuery, "Bearer " + strAccessToken);
            string strTotalSize = objTotalRecord["totalSize"].ToString();

            if (Int32.Parse(strTotalSize) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
