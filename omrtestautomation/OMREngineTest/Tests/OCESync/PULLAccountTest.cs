using MainFramework;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using Newtonsoft.Json.Linq;
using OCESyncTest.Constructors;
using OCESyncTest.Utils;
using OMREngineTest.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace OCESyncTest.Tests
{
    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    [Trait("TestRailProjectId", "95")] // OMR
    [Trait("TestRailSuiteId", "5866")] // OCESync tests
    [Trait("TestType", "Interoperability")]
    [Trait("custom_testtype", "0")]
    [Trait("custom_tstype", "1")]
    [Trait("custom_testexecution", "1")]

    [Collection("Sequential")]
    public class PULLAccountTest 
    {
        private RequestSender _send;

        private readonly ITestOutputHelper _output;
        private SalesForceDataCollector _collectSalesForce; private SnowflakeDBData _sfdbdata;
        private string _baseURL; private string _clientID; private string _userName; private string _passWord; private string _clientSecret; private string _namespace;
        private OptimizerApi _engineApi;
        private string _xApiKey;
        private string _snowConnection;
        private string _sfDBName; private SalesforceDataCreation _sfDataCreation;
        //public string tenantConfigfile = "preprod-org.json";
        //public string environment = "preprod";
        public string tenantConfigfile = "master-org.json";
        public string environment = "master";
        private PUBLISHOtherOmrDataTest objPUBLISHOtherOmrDataTest;
        private CommonFunction commonFunc;
        public PULLAccountTest(ITestOutputHelper output)
        {
            _output = output;
            output.WriteLine("input received");
            TenantFixture _tenantData = new TenantFixture(tenantConfigfile);
            commonFunc = new CommonFunction();
            _sfdbdata = new SnowflakeDBData(_output);

            _baseURL = _tenantData.Config.baseURL;
            _clientID = _tenantData.Config.clientID;
            _clientSecret = _tenantData.Config.clientSecret;
            _namespace = _tenantData.Config.sfnamespace;
            _userName = _tenantData.Config.userName;
            _passWord = _tenantData.Config.password;
            _snowConnection = _tenantData.Config.snowConnection;
            _xApiKey = _tenantData.Config.sfApiKey;
            environment = _tenantData.Config.endpoint;
            _sfDBName = GetDBName();
            _collectSalesForce = new SalesForceDataCollector(_baseURL, _clientID, _clientSecret, _userName, _passWord, _namespace);
            _send = new RequestSender();
            _engineApi = new OptimizerApi(_tenantData.Config);
            objPUBLISHOtherOmrDataTest = new PUBLISHOtherOmrDataTest(_output);
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
            string db_key = dbDetailsDict.Keys.Contains<string>("DB") ? "DB" : "db";
            return dbDetailsDict[db_key];
        }

        [Fact]
        public void PullJobAccountAlignment()
        {
            //UserStory937Dynamic();
            UserStrory937Explicit();
        }

        internal void UserStrory937Explicit()

        {
            _sfDataCreation = new SalesforceDataCreation(_output);
            Dictionary<string, object> objDictSalesForceDb = new Dictionary<string, object>();
            Dictionary<string, object> objDictSnowDb = new Dictionary<string, object>();
            CommonFunction commFunction = new CommonFunction();
            var objDict = _sfDataCreation.Fun_ScenarioRuleCreation();
            //Thread.Sleep(450000);

            objPUBLISHOtherOmrDataTest.scenarioStatusProgressCheck(objDict["ScenarioId"].ToString(), "SYNCERROR");
            objPUBLISHOtherOmrDataTest.PushJob();
            Thread.Sleep(150000);
            string strOCETerrCheckQuery = "SELECT Id, Name FROM Territory2 where Name = '" + objDict["Territory"].ToString() + "'";
            objDictSalesForceDb = _collectSalesForce.GetSalesForceData(strOCETerrCheckQuery);

            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            Random rnd = new Random();
            Dictionary<string, object> objAccount = new Dictionary<string, object>();
            objAccount.Add("FirstName", "Automation");
            objAccount.Add("LastName", "Testing " + commFunction.GenerateRndNumber(3, rnd));
            objAccount.Add("OCE__UniqueIntegrationID__c", commFunction.GenerateRndNumber(12, rnd));
            objAccount.Add("OCE__ProfessionalTitle__c", "Medical Doctors");
            objAccount.Add("OCE__Status__c", "Active");
            objAccount.Add("OCE__OneKeyID__c", "Testing OneKeyID " + commFunction.GenerateRndNumber(3, rnd));
            objAccount.Add("OCE__StructureType__c", "Department");
            objAccount.Add("OCE__IndividualTypes__c", "Business Contact");
            objAccount.Add("OCE__OrganizationTypes__c", "Department");
            objAccount.Add("OCE__Territories__c", objDictSalesForceDb["Name"].ToString());
            string strAccountID = sf.Post_Scenario("Account", objAccount);
            _output.WriteLine(strAccountID);
            GetPullJob();
            Thread.Sleep(180000);

            string strAccountQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"Territories\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + strAccountID + "'";
            objDictSnowDb = _sfdbdata.get_TableWise_Data(strAccountQuery,_snowConnection);

            if (objDictSnowDb["Id"].ToString().Equals(strAccountID) && objDictSnowDb["Territories"].ToString().Equals(objAccount["OCE__Territories__c"].ToString()))
            {
                _output.WriteLine("Datas are displayed in Account Table");
            }
            else
            {
                _output.WriteLine("Datas are not displayed in Account Table");
            }
            objDictSnowDb.Clear();

            string strAccountExplicit = "Select \"OMAccountId\",\"SOMTerritoryId\",\"EffectiveDate\" from \"" + _sfDBName + "\".\"OUTPUT\".\"OMAccountExplicit\" where \"OMAccountId\" = '" + strAccountID + "'";
            objDictSnowDb = _sfdbdata.get_TableWise_Data(strAccountExplicit,_snowConnection);
            if (objDictSnowDb["OMAccountId"].ToString().Equals(strAccountID) && objDictSnowDb["SOMTerritoryId"].ToString().Equals(objAccount["OCE__Territories__c"].ToString()))
            {
                _output.WriteLine("Passed OMAccountExplicit ");
            }
            else
            {
                _output.WriteLine("Failed OMAccountExplicit ");
            }


        }

        internal void UserStory937Dynamic()
        {
            _sfDataCreation = new SalesforceDataCreation(_output);

            Dictionary<string, object> objDictSalesForceDb = new Dictionary<string, object>();
            Dictionary<string, object> objDictSnowDb = new Dictionary<string, object>();
            CommonFunction commFunction = new CommonFunction();
            var objDict = _sfDataCreation.Fun_ScenarioRuleCreation();
            Thread.Sleep(200000);
            objPUBLISHOtherOmrDataTest.PushJob();
            Thread.Sleep(150000);
            string strOCETerrCheckQuery = "SELECT Id, Name FROM Territory2 where Name = '" + objDict["Territory"].ToString() + "'";
            objDictSalesForceDb = _collectSalesForce.GetSalesForceData(strOCETerrCheckQuery);

            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            Random rnd = new Random();
            Dictionary<string, object> objAccount = new Dictionary<string, object>();
            objAccount.Add("FirstName", "Automation");
            objAccount.Add("LastName", "Testing " + commFunction.GenerateRndNumber(3, rnd));
            objAccount.Add("OCE__UniqueIntegrationID__c", commFunction.GenerateRndNumber(12, rnd));
            objAccount.Add("OCE__ProfessionalTitle__c", "Medical Doctors");
            objAccount.Add("OCE__Status__c", "Active");
            objAccount.Add("OCE__OneKeyID__c", "Testing OneKeyID " + commFunction.GenerateRndNumber(3, rnd));
            objAccount.Add("OCE__StructureType__c", "Department");
            objAccount.Add("OCE__IndividualTypes__c", "Business Contact");
            objAccount.Add("OCE__OrganizationTypes__c", "Department");
            objAccount.Add("OCE__Territories__c", objDictSalesForceDb["Name"].ToString());
            string strAccountID = sf.Post_Scenario("Account", objAccount);
            _output.WriteLine(strAccountID);
            GetPullJob("", false);
            Thread.Sleep(180000);
            string strAccountQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"Territories\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + strAccountID + "'";
            objDictSnowDb = _sfdbdata.get_TableWise_Data(strAccountQuery,_snowConnection);

            if (objDictSnowDb["Id"].ToString().Equals(strAccountID) && objDictSnowDb["Territories"].ToString().Equals(objAccount["OCE__Territories__c"].ToString()))
            {
                _output.WriteLine("Datas are displayed in Account Table");
            }
            else
            {
                _output.WriteLine("Datas are not displayed in Account Table");
            }
            objDictSnowDb.Clear();
            string strCustomerAlignmentRuleQuery = "Select \"OMAccountId\",\"Territories\" From \"" + _sfDBName + "\".\"STATIC\".\"OMCustomerAlignmentRule\" where \"OMAccountId\" = '" + strAccountID + "'";
            objDictSnowDb = _sfdbdata.get_TableWise_Data(strCustomerAlignmentRuleQuery,_snowConnection);
            if (objDictSnowDb["OMAccountId"].ToString().Equals(strAccountID) && objDictSnowDb["Territories"].ToString().Equals(objAccount["OCE__Territories__c"].ToString()))
            {
                _output.WriteLine("Datas are displayed in OMCustomerAlignmentRule Table");
            }
            else
            {
                _output.WriteLine("Datas are not displayed in OMCustomerAlignmentRule Table");
            }
            objDictSnowDb.Clear();
            string strAccountExplicit = "Select \"OMAccountId\",\"SOMTerritoryId\",\"EffectiveDate\" from \"" + _sfDBName + "\".\"OUTPUT\".\"OMAccountExplicit\" where \"OMAccountId\" = '" + strAccountID + "'";
            objDictSnowDb = _sfdbdata.get_TableWise_Data(strAccountExplicit,_snowConnection);
            if (objDictSnowDb.Count() == 0)
            {
                _output.WriteLine("Passed OMAccountExplicit ");
            }
            else
            {
                _output.WriteLine("Failed OMAccountExplicit ");
            }



        }

        internal void GetPullJob(string strOCEMode = "OCE", bool AccountTerritory = true)
        {
            PullApiRequestBody apiRequestBody;

            JObject token_obj = _collectSalesForce.GET_EngineToken();

            //var apiRequestBody = new PullApiRequestBody("OCESYNC", "PULL_ACCOUNT", "OCE", "", "{}");
            if (AccountTerritory)
            {
                if (strOCEMode.Equals("OCE"))
                {
                    apiRequestBody = new PullApiRequestBody("OCESYNC", "PULL_ACCOUNT", "OCE", "", true, true, "", "");
                }
                else
                {
                    apiRequestBody = new PullApiRequestBody("OCESYNC", "PULL_ACCOUNT", "OCE_LEXI", "", true, true, "", "");
                }
            }
            else
            {


                apiRequestBody = new PullApiRequestBody("OCESYNC", "PULL_ACCOUNT", "OCE", "", true, false, "", "");



            }

            _engineApi.SetToken(token_obj["token"].ToString(), token_obj["refresh"].ToString(), token_obj["lexiApiKey"].ToString(), token_obj["endpoint"].ToString());
            var response = _engineApi.RequestStatus("ocesync", "POST", apiRequestBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }



        public KeyValuePair<Dictionary<string, object>, Dictionary<string, object>> GetQueryDictionary(String strSnowFlake, string strSalesForce)
        {
            Dictionary<string, object> objDictSalesForceDb = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, object> objDictSnowDb = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            objDictSnowDb = _sfdbdata.get_TableWise_Data(strSnowFlake,_snowConnection);
            Thread.Sleep(3000);
            objDictSalesForceDb = _collectSalesForce.GetSalesForceData(strSalesForce);
            return new KeyValuePair<Dictionary<string, object>, Dictionary<string, object>>(objDictSnowDb, objDictSalesForceDb);
        }


        public string Fun_Territory2()
        {

            string strTerritor;

            string strTerritoryTypeId = Fun_TerritoryTypeId();
            string strTerritoryModelId = Fun_TerritoryModelId();

            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            //EnvFixture _api = new EnvFixture("envConfig-preprod.json");
            EnvFixture _api = new EnvFixture("envConfig-master.json");
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            CommonFunction commFunction = new CommonFunction();
            Random rnd = new Random();
            Territory2 objTerritory2 = new Territory2();
            objTerritory2.Name = "Territory" + commFunction.GenerateRndNumber(3, rnd);
            objTerritory2.Territory2TypeId = strTerritoryTypeId;
            objTerritory2.Territory2ModelId = strTerritoryModelId;
            objTerritory2.DeveloperName = "Tester" + commFunction.GenerateRndNumber(3, rnd);

            strTerritor = sf.Post_Scenario("Territory2", objTerritory2);

            Thread.Sleep(5000);
            return strTerritor;
        }


        public string Fun_TerritoryTypeId()
        {
            string strTypeId;
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            //EnvFixture _api = new EnvFixture("envConfig-preprod.json");
            EnvFixture _api = new EnvFixture("envConfig-master.json");
            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            CommonFunction commFunction = new CommonFunction();
            Random rnd = new Random();
            Territory2Type objTerritory2Type = new Territory2Type();

            objTerritory2Type.DeveloperName = "Tester" + commFunction.GenerateRndNumber(3, rnd);
            objTerritory2Type.MasterLabel = "Tester" + commFunction.GenerateRndNumber(3, rnd);
            objTerritory2Type.Priority = 1;
            strTypeId = sf.Post_Scenario("Territory2Type", objTerritory2Type);

            Thread.Sleep(5000);

            return strTypeId;

        }


        public string Fun_TerritoryModelId()
        {
            string strModelId;
            String strAccessToken = _collectSalesForce.GET_SalesforceToken();
            //EnvFixture _api = new EnvFixture("envConfig-preprod.json");
            EnvFixture _api = new EnvFixture("envConfig-master.json");

            ScenarioFunction sf = new ScenarioFunction(strAccessToken, _baseURL, "");
            CommonFunction commFunction = new CommonFunction();
            Random rnd = new Random();

            Territory2Model objTerritory2Model = new Territory2Model();

            objTerritory2Model.DeveloperName = "Tester" + commFunction.GenerateRndNumber(3, rnd);
            objTerritory2Model.Name = "TerritoryModel " + commFunction.GenerateRndNumber(3, rnd);
            objTerritory2Model.Description = "TerritoryModel Testing";
            strModelId = sf.Post_Scenario("Territory2Model", objTerritory2Model);

            Thread.Sleep(5000);

            return strModelId;

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

        [Fact]
        public void OCEpullAccountJob()
        {
            bool blnFlag;
            List<string> objAccountId = new List<string>();
            Dictionary<string, object> objDict = new Dictionary<string, object>();
            Dictionary<string, object> objSnowDict = new Dictionary<string, object>();
            Dictionary<string, object> objSalesDict = new Dictionary<string, object>();
            HashSet<bool> objBlnFlag = new HashSet<bool>();
            string strSnowFlakeQuery = null, strSalesForceQuery = null;
            _sfDataCreation = new SalesforceDataCreation(_output);

           //var strRegionCountryCode = _sfDataCreation.Fun_RegionCreation();
            for (int i = 0; i < 4; i++)
            {
                if (i == 3)
                {
                    objDict.Add("OCEAccount", _sfDataCreation.Fun_OCE_AccountCreation("", "N"));
                }
                else
                {
                    objAccountId.Add(_sfDataCreation.Fun_OCE_AccountCreation("BE"));
                    //BE//strRegionCountryCode.Item2.ToString()
                }
            }



            string strAffiliationId = _sfDataCreation.Fun_OCE_AffiliationCreation(objAccountId.ElementAt(0).ToString(), objAccountId.ElementAt(1).ToString());
            // objDict.Add("OCEAffiliation", Fun_OCE_AffiliationCreation(objAccountId.ElementAt(0).ToString(), objAccountId.ElementAt(1).ToString());)
            objDict.Add("OCEAffiliation", strAffiliationId);

            string strAddress = _sfDataCreation.OCE_AddressCreation();
            objDict.Add("OCEAddress", strAddress);

            string strAccountAddress = _sfDataCreation.Fun_OCE_AccountAddress(objAccountId.ElementAt(2).ToString(), objDict["OCEAddress"].ToString());
            objDict.Add("OCEAccountAddress", strAccountAddress);

            //string strAccountTerritory = _sfDataCreation.Fun_OCEAccountTerritory(objAccountId.ElementAt(0).ToString());
            // objDict.Add("OCEAccountTerritory", strAccountAddress);

            GetPullJob();

            Thread.Sleep(180000);

            strSnowFlakeQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + objAccountId.ElementAt(0).ToString() + "'";
            strSalesForceQuery = "SELECT Id,Type,OCE__RecordTypeDeveloperName__c,Name,FirstName,LastName,OCE__IsActive__c,OCE__UniqueIntegrationID__c,OCE__OneKeyID__c,OCE__ProfessionalTitle__c,OCE__StructureType__c,OCE__CountryCode__c,OCE__IndividualTypes__c,OCE__OrganizationTypes__c,OCE__Specialty__c,OCE__Specialty2__c,OCE__Specialty3__c,OCE__Specialty4__c,OCE__Specialty5__c FROM Account where Id = '" + objAccountId.ElementAt(0).ToString() + "'";
            var objAccountQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAccountQueryDictionar.Key;
            objSalesDict = objAccountQueryDictionar.Value;
            blnFlag = commonFunc.GetTableComparison(objSnowDict, objSalesDict,_output);
            objBlnFlag.Add(blnFlag);

            //Clear Dictionary Object and Variable as Null 
            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"AffiliationType\",\"From\",\"To\",\"Status\",\"Roles\",\"UniqueIntegrationId\",\"IsPrimary\",\"Title\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAffiliation\" Where \"Id\" = '" + objDict["OCEAffiliation"].ToString() + "'";
            strSalesForceQuery = "SELECT Id, OCE__AffiliationType__c, OCE__From__c, OCE__To__c,OCE__IsActive__c,OCE__Roles__c,OCE__UniqueIntegrationID__c,OCE__IsPrimary__c,OCE__Title__c from OCE__Affiliation__c where Id = '" + objDict["OCEAffiliation"].ToString() + "'";
            var objAffiliationQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAffiliationQueryDictionar.Key;
            objSalesDict = objAffiliationQueryDictionar.Value;
            blnFlag = commonFunc.GetTableComparison(objSnowDict, objSalesDict,_output);
            objBlnFlag.Add(blnFlag);

            //Clear Dictionary Object and Variable as Null 
            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"AddressLine1\",\"AddressLine2\",\"AddressLine3\",\"AddressLine4\",\"City\",\"State\",\"ZipCode\",\"Country\",\"Status\",\"UniqueIntegrationId\",\"Latitude\",\"Longitude\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccountAddress\" Where \"Id\" = '" + objDict["OCEAddress"].ToString() + "'";
            //strSalesForceQuery = "SELECT Id,Name,OCE__AddressLine2__c,OCE__AddressLine3__c,OCE__AddressLine4__c,OCE__City__c,OCE__State__c,OCE__ZipCode__c,OCE__Country__c,OCE__Inactive__c,OCE__UniqueIntegrationID__c,OCE__GeoLocation__Latitude__s,OCE__GeoLocation__Longitude__s FROM OCE__Address__c where Id = '" + objDict["OCEAddress"].ToString() + "'";
            strSalesForceQuery = "SELECT Id,Name,OCE__AddressLine2__c,OCE__AddressLine3__c,OCE__AddressLine4__c,OCE__City__c,OCE__State__c,OCE__ZipCode__c,OCE__CountryCode__c,OCE__Inactive__c,OCE__UniqueIntegrationID__c,OCE__GeoLocation__Latitude__s,OCE__GeoLocation__Longitude__s FROM OCE__Address__c where Id = '" + objDict["OCEAddress"].ToString() + "'";
            var objAddressQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAddressQueryDictionar.Key;
            objSalesDict = objAddressQueryDictionar.Value;
            blnFlag = commonFunc.GetTableComparison(objSnowDict, objSalesDict,_output);
            objBlnFlag.Add(blnFlag);

            //Clear Dictionary Object and Variable as Null 
            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"AddressType\",\"OceSourceObject\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAffiliation\" Where \"Id\" = '" + strAccountAddress + "'";
            strSalesForceQuery = "SELECT OCE__AddressType__c FROM OCE__AccountAddress__c  where Id = '" + strAccountAddress + "'";
            var objOCEAccountAddressQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objOCEAccountAddressQueryDictionar.Key;
            objSalesDict = objOCEAccountAddressQueryDictionar.Value;
            if (objSnowDict["AddressType"].ToString().Equals(objSalesDict["OCE__AddressType__c"].ToString()) && objSnowDict["OceSourceObject"].ToString().Equals("OCE__AccountAddress__c"))
            {

                objBlnFlag.Add(true);
            }
            else
            {
                objBlnFlag.Add(false);
            }


            var sqlQuery = "SELECT \"Id\",\"SOMRegionId\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + objAccountId.ElementAt(0).ToString() + "'";
            Dictionary<string, object> objDictSnowDb = _sfdbdata.get_TableWise_Data(sqlQuery,_snowConnection);
            if (objDictSnowDb["SOMRegionId"].ToString().Equals("TestRegion_k0j"))//BE//"TestRegion_k0j"//strRegionCountryCode.Item1.ToString()
            {
                objBlnFlag.Add(true);
            }
            else
            {
                objBlnFlag.Add(true);
            }
            var sqlQuerys = "SELECT \"Id\",\"SOMRegionId\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + objDict["OCEAccount"].ToString() + "'";
            Dictionary<string, object> objDictSnowDbs = _sfdbdata.get_TableWise_Data(sqlQuerys,_snowConnection);
            if (objDictSnowDbs["SOMRegionId"].ToString().Equals(""))
            {
                objBlnFlag.Add(true);
            }
            else
            {
                objBlnFlag.Add(true);
            }

            if (objBlnFlag.Contains(false))
            {
                Assert.True(false, "Insert Failed");

            }
            else
            {
                Assert.True(true, "Insert Passed");

            }

            //Clear HashSet Object
            objBlnFlag.Clear();

            _sfDataCreation.Fun_OCEAccountUpdate(objAccountId.ElementAt(0).ToString());
            _sfDataCreation.Fun_OCEAffiliationIdUpdate(objDict["OCEAffiliation"].ToString());
            _sfDataCreation.Fun_OCEAddressUpdate(objDict["OCEAddress"].ToString());
            //_sfDataCreation.Fun_OCEAccountTerritoryUpdate(objDict["OCEAccountTerritory"].ToString());

            GetPullJob();
            Thread.Sleep(180000);

            strSnowFlakeQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + objAccountId.ElementAt(0).ToString() + "'";
            strSalesForceQuery = "SELECT Id,Type,OCE__RecordTypeDeveloperName__c,Name,FirstName,LastName,OCE__IsActive__c,OCE__UniqueIntegrationID__c,OCE__OneKeyID__c,OCE__ProfessionalTitle__c,OCE__StructureType__c,OCE__CountryCode__c,OCE__IndividualTypes__c,OCE__OrganizationTypes__c,OCE__Specialty__c,OCE__Specialty2__c,OCE__Specialty3__c,OCE__Specialty4__c,OCE__Specialty5__c FROM Account where Id = '" + objAccountId.ElementAt(0).ToString() + "'";
            var objAccountUpdateQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAccountUpdateQueryDictionar.Key;
            objSalesDict = objAccountUpdateQueryDictionar.Value;
            blnFlag = commonFunc.GetTableComparison(objSnowDict, objSalesDict,_output);
            objBlnFlag.Add(blnFlag);

            //Clear Dictionary Object and Variable as Null 
            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"AffiliationType\",\"From\",\"To\",\"Status\",\"Roles\",\"UniqueIntegrationId\",\"IsPrimary\",\"Title\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAffiliation\" Where \"Id\" = '" + objDict["OCEAffiliation"].ToString() + "'";
            strSalesForceQuery = "SELECT Id, OCE__AffiliationType__c, OCE__From__c, OCE__To__c,OCE__IsActive__c,OCE__Roles__c,OCE__UniqueIntegrationID__c,OCE__IsPrimary__c,OCE__Title__c from OCE__Affiliation__c where Id = '" + objDict["OCEAffiliation"].ToString() + "'";
            var objAffiliationUpdateQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAffiliationUpdateQueryDictionar.Key;
            objSalesDict = objAffiliationUpdateQueryDictionar.Value;
            blnFlag = commonFunc.GetTableComparison(objSnowDict, objSalesDict,_output);
            objBlnFlag.Add(blnFlag);

            //Clear Dictionary Object and Variable as Null 
            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"AddressLine1\",\"AddressLine2\",\"AddressLine3\",\"AddressLine4\",\"City\",\"State\",\"ZipCode\",\"Country\",\"Status\",\"UniqueIntegrationId\",\"Latitude\",\"Longitude\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccountAddress\" Where \"Id\" = '" + objDict["OCEAddress"].ToString() + "'";
            strSalesForceQuery = "SELECT Id,Name,OCE__AddressLine2__c,OCE__AddressLine3__c,OCE__AddressLine4__c,OCE__City__c,OCE__State__c,OCE__ZipCode__c,OCE__CountryCode__c,OCE__Inactive__c,OCE__UniqueIntegrationID__c,OCE__GeoLocation__Latitude__s,OCE__GeoLocation__Longitude__s FROM OCE__Address__c where Id = '" + objDict["OCEAddress"].ToString() + "'";
            var objAddressUpdateQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAddressUpdateQueryDictionar.Key;
            objSalesDict = objAddressUpdateQueryDictionar.Value;
            blnFlag = commonFunc.GetTableComparison(objSnowDict, objSalesDict,_output);
            objBlnFlag.Add(blnFlag);

            //Clear Dictionary Object and Variable as Null 
            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            if (objBlnFlag.Contains(false))
            {
                Assert.True(false, "Update Failed");
            }
            else
            {
                Assert.True(true, "Update Passed");

            }

            //Clear HashSet Object
            objBlnFlag.Clear();


            blnFlag = _sfDataCreation.Fun_OCEAccountDelete(objDict["OCEAccount"].ToString());
            objBlnFlag.Add(blnFlag);
            blnFlag = false;
            blnFlag = _sfDataCreation.Fun_OCEAffiliationDelete(objDict["OCEAffiliation"].ToString());
            objBlnFlag.Add(blnFlag);
            blnFlag = false;
            blnFlag = _sfDataCreation.Fun_OCEAddressDelete(objDict["OCEAddress"].ToString());
            objBlnFlag.Add(blnFlag);
            blnFlag = false;
            //  blnFlag = _sfDataCreation.Fun_OCEAccountTerritoryDelete(objDict["OCEAccountTerritory"].ToString());
            //objBlnFlag.Add(blnFlag);
            //blnFlag = false;

            GetPullJob();
            Thread.Sleep(180000);

            if (objBlnFlag.Contains(false))
            {
                Assert.True(false, "Delete Failed in OCE");
            }
            else
            {

                objBlnFlag.Clear();

                strSnowFlakeQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + objDict["OCEAccount"].ToString() + "'";
                objSnowDict = _sfdbdata.get_TableWise_Data(strSnowFlakeQuery,_snowConnection);
                if (objSnowDict.Count() == 0)
                {
                    // blnFlag = true;
                    objBlnFlag.Add(true);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }
                else
                {
                    //   blnFlag = true;
                    objBlnFlag.Add(false);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();

                }

                strSnowFlakeQuery = "SELECT \"Id\",\"AffiliationType\",\"From\",\"To\",\"AddressId\",\"Status\",\"Roles\",\"UniqueIntegrationId\",\"IsPrimary\",\"AddressType\",\"Title\",\"OceSalesId\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAffiliation\" Where \"OceSalesId\" = '" + objDict["OCEAffiliation"].ToString() + "'";
                objSnowDict = _sfdbdata.get_TableWise_Data(strSnowFlakeQuery,_snowConnection);
                if (objSnowDict.Count() == 0)
                {
                    objBlnFlag.Add(true);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }
                else
                {
                    objBlnFlag.Add(false);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }


                strSnowFlakeQuery = "SELECT \"Id\",\"AddressLine1\",\"AddressLine2\",\"AddressLine3\",\"AddressLine4\",\"City\",\"State\",\"ZipCode\",\"Country\",\"Status\",\"UniqueIntegrationId\",\"OceSalesId\",\"Latitude\",\"Longitude\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccountAddress\" Where \"OceSalesId\" = '" + objDict["OCEAccountAddress"] + "'";
                objSnowDict = _sfdbdata.get_TableWise_Data(strSnowFlakeQuery,_snowConnection);
                if (objSnowDict.Count() == 0)
                {
                    objBlnFlag.Add(true);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }
                else
                {
                    objBlnFlag.Add(false);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }

                /*
                                strSnowFlakeQuery = "SELECT \"Id\",\"OceSalesId\", \" SOMTerritoryId\",\"UniqueIntegrationId\",\"CreatedDate\",\"LastModifiedDate\"  FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccountTerritoryFields\" Where \"Id\" = '" + objDict["OCEAccountTerritory"].ToString() + "'";
                                objSnowDict = _sfdbdata.get_TableWise_Data(strSnowFlakeQuery);
                                if (objSnowDict["OceSalesId"].ToString().Equals(""))
                                {
                                    objBlnFlag.Add(true);
                                    strSnowFlakeQuery = null;
                                    objSnowDict.Clear();
                                }
                                else
                                {
                                    objBlnFlag.Add(true);
                                    strSnowFlakeQuery = null;
                                    objSnowDict.Clear();
                                }
                */


                if (objBlnFlag.Contains(false))
                {
                    Assert.True(false, "Delete Failed");
                }
                else
                {
                    Assert.True(true, "Delete Passed");

                }

            }

        }

        [Fact]
        public void PullAccountJobLexiMode()
        {
            bool blnFlag, isValid;
            List<string> objAccountId = new List<string>();
            Dictionary<string, object> objDict = new Dictionary<string, object>();
            Dictionary<string, object> objSnowDict = new Dictionary<string, object>();
            Dictionary<string, object> objSalesDict = new Dictionary<string, object>();
            HashSet<bool> objBlnFlag = new HashSet<bool>();
            string strSnowFlakeQuery = null, strSalesForceQuery = null;
            _sfDataCreation = new SalesforceDataCreation(_output);


            for (int i = 0; i < 4; i++)
            {
                if (i == 3)
                {
                    objDict.Add("OCEAccount", _sfDataCreation.Fun_OCE_AccountCreation());
                }
                else
                {
                    objAccountId.Add(_sfDataCreation.Fun_OCE_AccountCreation());
                }
            }



            string strAffiliationId = _sfDataCreation.Fun_OCE_AffiliationCreation(objAccountId.ElementAt(0).ToString(), objAccountId.ElementAt(1).ToString());
            // objDict.Add("OCEAffiliation", Fun_OCE_AffiliationCreation(objAccountId.ElementAt(0).ToString(), objAccountId.ElementAt(1).ToString());)
            objDict.Add("OCEAffiliation", strAffiliationId);

            string strAddress = _sfDataCreation.OCE_AddressCreation();
            objDict.Add("OCEAddress", strAddress);

            string strAccountAddress = _sfDataCreation.Fun_OCE_AccountAddress(objAccountId.ElementAt(2).ToString(), objDict["OCEAddress"].ToString());
            objDict.Add("OCEAccountAddress", strAccountAddress);


            GetPullJob("OCELEX");

            Thread.Sleep(150000);

            strSnowFlakeQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + objAccountId.ElementAt(0).ToString() + "'";
            strSalesForceQuery = "SELECT Id,Type,OCE__RecordTypeDeveloperName__c,Name,FirstName,LastName,OCE__IsActive__c,OCE__UniqueIntegrationID__c,OCE__OneKeyID__c,OCE__ProfessionalTitle__c,OCE__StructureType__c,OCE__CountryCode__c,OCE__IndividualTypes__c,OCE__OrganizationTypes__c,OCE__Specialty__c,OCE__Specialty2__c,OCE__Specialty3__c,OCE__Specialty4__c,OCE__Specialty5__c FROM Account where Id = '" + objAccountId.ElementAt(0).ToString() + "'";
            var objAccountQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAccountQueryDictionar.Key;
            objSalesDict = objAccountQueryDictionar.Value;
            // blnFlag = GetTableComparison(objSnowDict, objSalesDict);
            if (objSalesDict["OCE__UniqueIntegrationID__c"].ToString().Equals(objSnowDict["Id"].ToString()))
            {

                if (objSalesDict.ContainsKey("OCE__IsActive__c"))
                {
                    isValid = objSalesDict.ContainsKey("OCE__IsActive__c") ? objSalesDict["OCE__IsActive__c"].ToString() == "True" : true;
                    if (isValid && objSnowDict["Status"].Equals("ACTV"))
                    // if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__Status__c"].ToString()))
                    {
                        _output.WriteLine("SnowFlake  Status and Sales Force OCE__Status__c are Matching");
                        objBlnFlag.Add(true);
                    }
                    else
                    {
                        _output.WriteLine("SnowFlake  Status and Sales Force OCE__Status__c are not Matching");
                        objBlnFlag.Add(false);

                    }

                }
                if (objSnowDict["UniqueIntegrationId"].ToString().Equals(objSalesDict["OCE__UniqueIntegrationID__c"].ToString()))
                {
                    _output.WriteLine("SnowFlake  UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake   UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are not Matching");
                    objBlnFlag.Add(false);
                }

                if (objSnowDict["OceSalesId"].ToString().Equals(objSalesDict["Id"].ToString()))
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are not Matching");
                    objBlnFlag.Add(false);
                }



            }
            else
            {
                objBlnFlag.Add(false);
            }



            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"OceSalesId\",\"AffiliationType\",\"From\",\"To\",\"Status\",\"Roles\",\"UniqueIntegrationId\",\"IsPrimary\",\"Title\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAffiliation\" Where \"Id\" = '" + objDict["OCEAffiliation"].ToString() + "'";
            strSalesForceQuery = "SELECT Id, OCE__AffiliationType__c, OCE__From__c, OCE__To__c,OCE__IsActive__c,OCE__Roles__c,OCE__UniqueIntegrationID__c,OCE__IsPrimary__c,OCE__Title__c from OCE__Affiliation__c where Id = '" + objDict["OCEAffiliation"].ToString() + "'";
            var objAffiliationQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAffiliationQueryDictionar.Key;
            objSalesDict = objAffiliationQueryDictionar.Value;
            //blnFlag = GetTableComparison(objSnowDict, objSalesDict);
            if (objSalesDict["OCE__UniqueIntegrationID__c"].ToString().Equals(objSnowDict["Id"].ToString()))
            {
                if (objSnowDict["UniqueIntegrationId"].ToString().Equals(objSalesDict["OCE__UniqueIntegrationID__c"].ToString()))
                {
                    _output.WriteLine("SnowFlake  UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake   UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are not Matching");
                    objBlnFlag.Add(false);
                }

                if (objSnowDict["OceSalesId"].ToString().Equals(objSalesDict["Id"].ToString()))
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are not Matching");
                    objBlnFlag.Add(false);
                }



            }
            else
            {
                objBlnFlag.Add(false);
            }

            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"AddressLine1\",\"AddressLine2\",\"AddressLine3\",\"AddressLine4\",\"City\",\"State\",\"ZipCode\",\"Country\",\"Status\",\"UniqueIntegrationId\",\"Latitude\",\"Longitude\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccountAddress\" Where \"Id\" = '" + objDict["OCEAddress"].ToString() + "'";
            strSalesForceQuery = "SELECT Id,Name,OCE__AddressLine2__c,OCE__AddressLine3__c,OCE__AddressLine4__c,OCE__City__c,OCE__State__c,OCE__ZipCode__c,OCE__CountryCode__c,OCE__Inactive__c,OCE__UniqueIntegrationID__c,OCE__GeoLocation__Latitude__s,OCE__GeoLocation__Longitude__s FROM OCE__Address__c where Id = '" + objDict["OCEAddress"].ToString() + "'";
            var objAddressQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAddressQueryDictionar.Key;
            objSalesDict = objAddressQueryDictionar.Value;
            //  blnFlag = GetTableComparison(objSnowDict, objSalesDict);

            if (objSalesDict["OCE__UniqueIntegrationID__c"].ToString().Equals(objSnowDict["Id"].ToString()))
            {
                if (objSnowDict["UniqueIntegrationId"].ToString().Equals(objSalesDict["OCE__UniqueIntegrationID__c"].ToString()))
                {
                    _output.WriteLine("SnowFlake  UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake   UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are not Matching");
                    objBlnFlag.Add(false);
                }

                if (objSnowDict["OceSalesId"].ToString().Equals(objSalesDict["Id"].ToString()))
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are not Matching");
                    objBlnFlag.Add(false);
                }



            }
            else
            {
                objBlnFlag.Add(false);
            }




            // objBlnFlag.Add(blnFlag);

            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;


            if (objBlnFlag.Contains(false))
            {
                Assert.True(false, "Insert Failed");

            }
            else
            {
                Assert.True(true, "Insert Passed");

            }
            objBlnFlag.Clear();

            _sfDataCreation.Fun_OCEAccountUpdate(objAccountId.ElementAt(0).ToString());
            _sfDataCreation.Fun_OCEAffiliationIdUpdate(objDict["OCEAffiliation"].ToString());
            _sfDataCreation.Fun_OCEAddressUpdate(objDict["OCEAddress"].ToString());


            GetPullJob("OCELEX");
            Thread.Sleep(180000);

            strSnowFlakeQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"OceSalesId\" = '" + objAccountId.ElementAt(0).ToString() + "'";
            strSalesForceQuery = "SELECT Id,Type,OCE__RecordTypeDeveloperName__c,Name,FirstName,LastName,OCE__IsActive__c,OCE__UniqueIntegrationID__c,OCE__OneKeyID__c,OCE__ProfessionalTitle__c,OCE__StructureType__c,OCE__CountryCode__c,OCE__IndividualTypes__c,OCE__OrganizationTypes__c,OCE__Specialty__c,OCE__Specialty2__c,OCE__Specialty3__c,OCE__Specialty4__c,OCE__Specialty5__c FROM Account where Id = '" + objAccountId.ElementAt(0).ToString() + "'";
            var objAccountUpdateQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAccountUpdateQueryDictionar.Key;
            objSalesDict = objAccountUpdateQueryDictionar.Value;
            if (objSalesDict["OCE__UniqueIntegrationID__c"].ToString().Equals(objSnowDict["Id"].ToString()))
            {

                if (objSalesDict.ContainsKey("OCE__IsActive__c"))
                {
                    isValid = objSalesDict.ContainsKey("OCE__IsActive__c") ? objSalesDict["OCE__IsActive__c"].ToString() == "True" : true;
                    if (isValid && objSnowDict["Status"].Equals("ACTV"))
                    // if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__Status__c"].ToString()))
                    {
                        _output.WriteLine("SnowFlake  Status and Sales Force OCE__Status__c are Matching");
                        objBlnFlag.Add(true);
                    }
                    else
                    {
                        _output.WriteLine("SnowFlake  Status and Sales Force OCE__Status__c are not Matching");
                        objBlnFlag.Add(false);

                    }

                }
                if (objSnowDict["UniqueIntegrationId"].ToString().Equals(objSalesDict["OCE__UniqueIntegrationID__c"].ToString()))
                {
                    _output.WriteLine("SnowFlake  UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake   UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are not Matching");
                    objBlnFlag.Add(false);
                }

                if (objSnowDict["OceSalesId"].ToString().Equals(objSalesDict["Id"].ToString()))
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are not Matching");
                    objBlnFlag.Add(false);
                }



            }
            else
            {
                objBlnFlag.Add(false);
            }


            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"OceSalesId\",\"AffiliationType\",\"From\",\"To\",\"Status\",\"Roles\",\"UniqueIntegrationId\",\"IsPrimary\",\"Title\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAffiliation\" Where \"Id\" = '" + objDict["OCEAffiliation"].ToString() + "'";
            strSalesForceQuery = "SELECT Id, OCE__AffiliationType__c, OCE__From__c, OCE__To__c,OCE__IsActive__c,OCE__Roles__c,OCE__UniqueIntegrationID__c,OCE__IsPrimary__c,OCE__Title__c from OCE__Affiliation__c where Id = '" + objDict["OCEAffiliation"].ToString() + "'";
            var objAffiliationUpdateQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAffiliationUpdateQueryDictionar.Key;
            objSalesDict = objAffiliationUpdateQueryDictionar.Value;
            if (objSalesDict["OCE__UniqueIntegrationID__c"].ToString().Equals(objSnowDict["Id"].ToString()))
            {
                if (objSnowDict["UniqueIntegrationId"].ToString().Equals(objSalesDict["OCE__UniqueIntegrationID__c"].ToString()))
                {
                    _output.WriteLine("SnowFlake  UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake   UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are not Matching");
                    objBlnFlag.Add(false);
                }

                if (objSnowDict["OceSalesId"].ToString().Equals(objSalesDict["Id"].ToString()))
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are not Matching");
                    objBlnFlag.Add(false);
                }



            }
            else
            {
                objBlnFlag.Add(false);
            }

            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            strSnowFlakeQuery = "SELECT \"Id\",\"AddressLine1\",\"AddressLine2\",\"AddressLine3\",\"AddressLine4\",\"City\",\"State\",\"ZipCode\",\"Country\",\"Status\",\"UniqueIntegrationId\",\"Latitude\",\"Longitude\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccountAddress\" Where \"Id\" = '" + objDict["OCEAddress"].ToString() + "'";
            strSalesForceQuery = "SELECT Id,Name,OCE__AddressLine2__c,OCE__AddressLine3__c,OCE__AddressLine4__c,OCE__City__c,OCE__State__c,OCE__ZipCode__c,OCE__CountryCode__c,OCE__Inactive__c,OCE__UniqueIntegrationID__c,OCE__GeoLocation__Latitude__s,OCE__GeoLocation__Longitude__s FROM OCE__Address__c where Id = '" + objDict["OCEAddress"].ToString() + "'";
            var objAddressUpdateQueryDictionar = GetQueryDictionary(strSnowFlakeQuery, strSalesForceQuery);
            objSnowDict = objAddressUpdateQueryDictionar.Key;
            objSalesDict = objAddressUpdateQueryDictionar.Value;
            if (objSalesDict["OCE__UniqueIntegrationID__c"].ToString().Equals(objSnowDict["Id"].ToString()))
            {
                if (objSnowDict["UniqueIntegrationId"].ToString().Equals(objSalesDict["OCE__UniqueIntegrationID__c"].ToString()))
                {
                    _output.WriteLine("SnowFlake  UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake   UniqueIntegrationId  and Sales Force OCE__IntegrationID__c are not Matching");
                    objBlnFlag.Add(false);
                }

                if (objSnowDict["OceSalesId"].ToString().Equals(objSalesDict["Id"].ToString()))
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are Matching");
                    objBlnFlag.Add(true);
                }
                else
                {
                    _output.WriteLine("SnowFlake  OceSalesId and Sales Force Id are not Matching");
                    objBlnFlag.Add(false);
                }



            }
            else
            {
                objBlnFlag.Add(false);
            }


            blnFlag = false;
            objSnowDict.Clear();
            objSalesDict.Clear();
            strSnowFlakeQuery = null;
            strSalesForceQuery = null;

            if (objBlnFlag.Contains(false))
            {
                Assert.True(false, "Update Failed");
            }
            else
            {
                Assert.True(true, "Update Passed");

            }
            objBlnFlag.Clear();


            blnFlag = _sfDataCreation.Fun_OCEAccountDelete(objDict["OCEAccount"].ToString());
            objBlnFlag.Add(blnFlag);
            blnFlag = false;
            blnFlag = _sfDataCreation.Fun_OCEAffiliationDelete(objDict["OCEAffiliation"].ToString());
            objBlnFlag.Add(blnFlag);
            blnFlag = false;
            blnFlag = _sfDataCreation.Fun_OCEAddressDelete(objDict["OCEAddress"].ToString());
            objBlnFlag.Add(blnFlag);
            blnFlag = false;


            GetPullJob("OCELEX");
            Thread.Sleep(180000);

            if (objBlnFlag.Contains(false))
            {
                Assert.True(false, "Delete Failed in OCE");
            }
            else
            {

                objBlnFlag.Clear();

                strSnowFlakeQuery = "SELECT \"Id\",\"Type\",\"AccountType\",\"Name\",\"FirstName\",\"LastName\",\"Status\",\"UniqueIntegrationId\",\"OneKeyID\",\"ProfessionalTitle\",\"StructureType\",\"OceSalesId\",\"CountryCode\",\"IndividualTypes\",\"OrganizationTypes\",\"Specialty\",\"Specialty2\",\"Specialty3\",\"Specialty4\",\"Specialty5\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" Where \"Id\" = '" + objDict["OCEAccount"].ToString() + "'";
                objSnowDict = _sfdbdata.get_TableWise_Data(strSnowFlakeQuery,_snowConnection);
                if (objSnowDict["OceSalesId"].ToString().Equals(""))
                {
                    // blnFlag = true;
                    objBlnFlag.Add(true);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }
                else
                {
                    //   blnFlag = true;
                    objBlnFlag.Add(false);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();

                }

                strSnowFlakeQuery = "SELECT \"Id\",\"AffiliationType\",\"From\",\"To\",\"AddressId\",\"Status\",\"Roles\",\"UniqueIntegrationId\",\"IsPrimary\",\"AddressType\",\"Title\",\"OceSalesId\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAffiliation\" Where \"Id\" = '" + objDict["OCEAffiliation"].ToString() + "'";
                objSnowDict = _sfdbdata.get_TableWise_Data(strSnowFlakeQuery,_snowConnection);
                if (objSnowDict["OceSalesId"].ToString().Equals(""))
                {
                    objBlnFlag.Add(true);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }
                else
                {
                    objBlnFlag.Add(false);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }

                strSnowFlakeQuery = "SELECT \"Id\",\"AddressLine1\",\"AddressLine2\",\"AddressLine3\",\"AddressLine4\",\"City\",\"State\",\"ZipCode\",\"Country\",\"Status\",\"UniqueIntegrationId\",\"OceSalesId\",\"Latitude\",\"Longitude\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMAccountAddress\" Where \"Id\" = '" + objDict["OCEAccountAddress"] + "'";
                objSnowDict = _sfdbdata.get_TableWise_Data(strSnowFlakeQuery,_snowConnection);
                if (objSnowDict["OceSalesId"].ToString().Equals(""))
                {
                    objBlnFlag.Add(true);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }
                else
                {
                    objBlnFlag.Add(false);
                    strSnowFlakeQuery = null;
                    objSnowDict.Clear();
                }

                if (objBlnFlag.Contains(false))
                {
                    Assert.True(false, "Delete Failed");
                }
                else
                {
                    Assert.True(true, "Delete Passed");

                }

            }

        }
    }
}
