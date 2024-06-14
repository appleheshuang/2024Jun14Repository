using MainFramework;
using MainFramework.GlobalUtils;
using Newtonsoft.Json.Linq;
using OCESyncTest.Constructors;
using OCESyncTest.Utils;
using OMREngineTest.Utils;
using OMREngineTest;
using ScenarioTests;
using SmokeTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
    public class PUBLISHCustomerAlignmentTest
    {
        private RequestSender _send;
        private readonly ITestOutputHelper _output;
        private SalesForceDataCollector _collectSalesForce;
        private FileHelper _filehelp; private SnowflakeDBData _sfdbData;
        private string _baseURL; private string _clientID; private string _userName; private string _passWord; private string _clientSecret;
        //public string tenantConfigFile = "preprod-org.json";
        public string tenantConfigFile = "master-org.json";
        public string environment = "master"; public PULLAccountTest pullTest;
        public Product smoke_product; public User smoke_user; public string ProductId; public Scenario smoke_scenario;
        public string UserId; public string RegionId; public User smoke_user2;
        public MainFramework.Region smoke_region; public SnowFlakeHelper _sfHelper;
        public ScenarioDefinition smoketest; public PUBLISHOtherOmrDataTest _pushjob;
        public string ScenarioId; public string SalesforceId; public string TerritoryId;
        private SnowFlakeBuildEntities _EntitiesCreation; public QuerySnowflake qsfc;
        private OptimizerApi _engineApi; public PULLAccountTest _pulltest; private string _snowConnection;
        private string _xApiKey;
        private string _sfDBName;


        public PUBLISHCustomerAlignmentTest(ITestOutputHelper output)
        {
            _output = output;
            output.WriteLine("input received");
            TenantFixture _tenantData = new TenantFixture(tenantConfigFile);
            _pushjob = new PUBLISHOtherOmrDataTest(_output);
            _filehelp = new FileHelper();

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
            _EntitiesCreation = new SnowFlakeBuildEntities(output);
            _sfHelper = new SnowFlakeHelper();
            _sfdbData = new SnowflakeDBData(_output);
            _engineApi = new OptimizerApi(_tenantData.Config);
             _pulltest = new PULLAccountTest(_output);
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


        internal void PushCustomerAlignment()
        {
            //API Request to insert a record in OCE Account - Salesforce DB

            JObject token_obj = _collectSalesForce.GET_EngineToken();

            //set scenario id
            var apiRequestBody = new PullApiRequestBody("OCESYNC", "PUBLISH_CUSTOMER_ALIGNMENT", "OCE", "", true, true, "", "");
            
            //run scenario

            _engineApi.SetToken(token_obj["token"].ToString(), token_obj["refresh"].ToString(), token_obj["lexiApiKey"].ToString(), token_obj["endpoint"].ToString());
            var response = _engineApi.RequestStatus("ocesync", "POST", apiRequestBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }


        [Fact]
        public void PublishCustomerAlignmentJob()
        {
            //Generating Token
            string accessToken = _collectSalesForce.GET_SalesforceToken();
            List<string> objSnowList;
            List<string> objSF;

            //Account creation in SalesfoeceDB
            string AccountId = _EntitiesCreation.Fun_OCE_AccountCreation();
            
            //Running pull job to sync data from salesforce to snowflakedb
            _pulltest.GetPullJob();
            Thread.Sleep(150000);

            //Verifying whether data synced to snowflake
            string accountCreationQuery = "select * from \"" + _sfDBName + "\".\"STATIC\".\"OMAccount\" where \"Id\" = '" + AccountId + "'";
            string SnowAccountId = _sfdbData.get_TableWise_Data(accountCreationQuery, "Id",_snowConnection);
            Assert.Equal(AccountId, SnowAccountId);

            Dictionary<string, List<string>> objdict = _EntitiesCreation.Scenariorule();
            
            var list = objdict.ToList<KeyValuePair<string, List<string>>>();
            List<string> objLi = new List<string>();

            //Verifying whether scenario synced or committed
            objLi = objdict["Scenario"];
            _output.WriteLine("Scenario : " + objLi[0]);

            _pushjob.scenarioStatusProgressCheck(objLi[0], "SYNCERROR");

            var strTerritories = new System.Text.StringBuilder();
            string AccountCreationQuery = "select * from \"" + _sfDBName + "\".\"OUTPUT\".\"OMAccountTerritory\" where \"OMAccountId\" = '" + AccountId + "'";
            HashSet<String> listTerritories = _sfdbData.get_TableWise_DataCount(AccountCreationQuery,_snowConnection);

            //Fetching all the territories alligned to the Account and storing in list
            for (int i = 0; i < listTerritories.Count(); i++)
            {
                _output.WriteLine("Territories are : " + listTerritories.ElementAt(i).ToString());
                strTerritories.Append(";" + listTerritories.ElementAt(i).ToString());

            }
            string allignedTerritories = strTerritories.ToString()+";";            
            _output.WriteLine("Territories are : " + allignedTerritories);

            //Running Publish job to push the territory data to OCE Db from OMR DB
            _pushjob.PushJob();
            Thread.Sleep(150000);

            //getting the Territory name
            objLi = objdict["Territory"];
            _output.WriteLine("Territory :" + objLi[1]);
            string SFTerritory4CreationQuery = "SELECT Id, Name FROM iq_sj_omr_01__OMScenarioTerritory__c where Id='" + objLi[1] + "'";
            string TerritoryName = _collectSalesForce.GetValue(accessToken, SFTerritory4CreationQuery, "Name");

            //Verifying whether Territory got synced in OCEDB
            string SFTerritoryCreationCheckQuery = "SELECT Id, Name, ParentTerritory2Id, LastModifiedDate FROM Territory2 where Name='" + TerritoryName + "'";
            string SFTerritoryName = _collectSalesForce.GetValue(accessToken, SFTerritoryCreationCheckQuery, "Name");
            Assert.Equal(TerritoryName, SFTerritoryName);

            //Running customerAllignment job
            PushCustomerAlignment();
            Thread.Sleep(150000);

            //Verifying whether customerAllignment rule data synced to OCEDB
            string sfCustomerAllignmentQuery = "SELECT Id, Name, OCE__Account__c, LastModifiedDate, CreatedDate, OCE__Territories__c, OCE__UniqueIntegrationID__c FROM OCE__CustomerAlignmentRule__c where OCE__Account__c = '"+ AccountId + "'";
            string SFCustomerTerritoryName = _collectSalesForce.GetValue(accessToken, sfCustomerAllignmentQuery, "OCE__Territories__c");

            //splitting the strings
            string[] arrSnowTerritorie = allignedTerritories.Split(";");
            string[] arrSFTerritorie = SFCustomerTerritoryName.Split(";");
            objSnowList = new List<string>(arrSnowTerritorie);

            //Sorting the retrieved strings in Ascending order for both DBs
            objSnowList.Sort();
            _output.WriteLine(objSnowList[2]);
            objSF = new List<string>(arrSFTerritorie);
            objSF.Sort();

            //comparing both Snowflakedb and SalesforceDB territories data are same
            if (objSnowList.SequenceEqual(objSF))
            {
                _output.WriteLine("Territories are Present in Both the Tables");
            }

            else
            {
                _output.WriteLine("Territories are not Present in Both the Tables");
            }


            //Deleting the record from AccountTerritory
            string deleteAccountTerritoryQuery = "delete from \"" + _sfDBName + "\".\"OUTPUT\".\"OMAccountTerritory\" where \"OMAccountId\" = '" + AccountId + "'";
            string SnowAccountTerritoryName = _sfdbData.get_TableWise_Data(deleteAccountTerritoryQuery, "SOMTerritoryId", _snowConnection);


            //Running pull job to sync data from salesforce to snowflakedb
            _pulltest.GetPullJob();
            Thread.Sleep(150000);

            //Running customerAllignment job
            PushCustomerAlignment();
            Thread.Sleep(150000);           

            //Verifying whether the territories are deleted in CustomerAllignmentRule table in OCEDB for particular AccountID on deleting the AccountTerritory record from snowflake
            string  sfCustomerAllignmentUpdateQuery = "SELECT Id, Name, OCE__Account__c, LastModifiedDate, CreatedDate, OCE__Territories__c, OCE__UniqueIntegrationID__c FROM OCE__CustomerAlignmentRule__c where OCE__Account__c = '" + AccountId + "'";
            string SFCustomerTerritoryUpdatedName = _collectSalesForce.GetValue(accessToken, sfCustomerAllignmentUpdateQuery, "OCE__Territories__c");
            Assert.Equal("", SFCustomerTerritoryUpdatedName);
            _output.WriteLine("Delete functionality verification is successful for CustomerAllignmentRule");

        }

    }

}
