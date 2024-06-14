using MainFramework;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using Newtonsoft.Json.Linq;
using OCESyncTest.Constructors;
using OCESyncTest.Utils;
using OMREngineTest.Utils;
using OMREngineTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class PUBLISHOtherOmrDataTest
    {
        private RequestSender _send;
        private readonly ITestOutputHelper _output;
        private SalesForceDataCollector _collectSalesForce;
        private SnowflakeDBData _sfdbData;
        private string _baseURL; private string _clientID; private string _userName; private string _passWord; private string _clientSecret;
        public Product smoke_product; public User smoke_user; public string ProductId; public Scenario smoke_scenario;
        //public string tenantConfigFile = "preprod-org.json";
        //public string environment = "preprod";
        public string tenantConfigFile = "master-org.json";
        public string environment = "master";
        public string UserId; public string RegionId; public User smoke_user2;
        public MainFramework.Region smoke_region; public SnowFlakeHelper _sfHelper;
        public ScenarioDefinition smoketest; public PULLAccountTest pullTest;
        public string ScenarioId; public string SalesforceId; public string TerritoryId;
        private SnowFlakeBuildEntities _EntitiesCreation; public QuerySnowflake qsfc;
        private OptimizerApi _engineApi;
        private string _xApiKey;
        private string _snowConnection;
        private CommonFunction commonFunc;
        private string _sfDBName;

        public PUBLISHOtherOmrDataTest(ITestOutputHelper output)
        {
            _output = output;
            output.WriteLine("input received");
            TenantFixture _tenantData = new TenantFixture(tenantConfigFile);
            commonFunc = new CommonFunction();
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


        internal void PushJob()
        {
            
            JObject token_obj = _collectSalesForce.GET_EngineToken();

            //set scenario id
            var apiRequestBody = new PullApiRequestBody("OCESYNC", "PUBLISH_OTHER_OMR_DATA", "OCE", "", true, true, "ETMInitialModel", "ETMInitialTerr2Type");
            //var body = JsonConvert.SerializeObject(apiRequestBody);

            //run scenario

            _engineApi.SetToken(token_obj["token"].ToString(), token_obj["refresh"].ToString(), token_obj["lexiApiKey"].ToString(), token_obj["endpoint"].ToString());
            var response = _engineApi.RequestStatus("ocesync", "POST", apiRequestBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }

        public string scenarioStatusProgressCheck(string ScenarioId, string expectedScenarioStatus)
        {
            //int timeInterval=1;
            string scenarioStatus = "";
            string sql = "select \"ScenarioStatus\" from \"STATIC\".\"OMScenario\" where \"Id\"='" + ScenarioId + "'";

            while (0 < 1)
            {
                scenarioStatus = _sfdbData.get_TableWise_Data(sql, "ScenarioStatus", _snowConnection);
                if (scenarioStatus.Equals(expectedScenarioStatus))
                {
                    break;
                }

            }
            return scenarioStatus;
        }


        [Fact]
        public void PublishOtherOMRDataJob()
        {
            string accessToken = _collectSalesForce.GET_SalesforceToken();

            Dictionary<string, List<string>> objdict = _EntitiesCreation.Scenariocommit();

            //Waiting for 10 mins to commit the scenario
            //Thread.Sleep(450000);


            var list = objdict.ToList<KeyValuePair<string, List<string>>>();

            List<string> objLi = new List<string>();

            //Verifying whether scenario synced or committed
            objLi = objdict["Scenario"];
            _output.WriteLine("Scenario : " + objLi[0]);

            scenarioStatusProgressCheck(objLi[0], "SYNCERROR");
            /*string SfSquery = "select * from \"" + _sfDBName + "\".\"STATIC\".\"OMScenario\" where \"Id\" = '" + objLi[0] + "'";
            _output.WriteLine("Query is: " + SfSquery);
            string scenarioStatus = _sfdbData.get_TableWise_Data(SfSquery, "ScenarioStatus", _snowConnection);
            Assert.Equal("SYNCERROR", scenarioStatus);*/

            //Updating the end date of Territory4
            objLi = objdict["Territory"];
            _output.WriteLine("Territory :" + objLi[3]);
            string SFTerritory4CreationQuery = "SELECT Id, Name FROM iq_sj_omr_01__OMScenarioTerritory__c where Id='" + objLi[3] + "'";
            string Territory4Name = _collectSalesForce.GetValue(accessToken, SFTerritory4CreationQuery, "Name");
            string SnowTerritory4Creationquery = "update \"" + _sfDBName + "\".\"OUTPUT\".\"OMTerritory\" set \"EffectiveDate\" = '" + DateTime.Now.AddDays(+2).ToString("yyyy-MM-dd") + "' where \"Name\" = '" + Territory4Name + "'";
            string Territory4EffectiveDate = _sfdbData.get_TableWise_Data(SnowTerritory4Creationquery, "EffectiveDate", _snowConnection);
            _output.WriteLine("Territory4 updated to future date");

            //Updating the end date of UserAssignment2
            objLi = objdict["UserAssignment"];
            _output.WriteLine("UserAssignment :" + objLi[1]);
            string salesUserAssignmentDateUpdate = "SELECT Id, Name, iq_sj_omr_01__TerritoryUniqueIntegrationId__c FROM iq_sj_omr_01__OMScenarioUserAssignment__c where Id='" + objLi[1] + "'";
            string UserAssignment2Name = _collectSalesForce.GetValue(accessToken, salesUserAssignmentDateUpdate, "iq_sj_omr_01__TerritoryUniqueIntegrationId__c");
            string snowUserAssignmentDateUpdate = "update \"" + _sfDBName + "\".\"OUTPUT\".\"OMUserAssignment\" set \"EffectiveDate\" = '" + DateTime.Now.ToString("yyyy-MM-dd") + "' , \"EndDate\" = '" + DateTime.Now.ToString("yyyy-MM-dd") + "'  where \"SOMTerritoryId\" = '" + UserAssignment2Name + "'";
            string UserAssignmentEndDate = _sfdbData.get_TableWise_Data(snowUserAssignmentDateUpdate, "EndDate", _snowConnection);

            //Updating the end date of UserAddress record
            objLi = objdict["UserAddress"];
            _output.WriteLine("userAddress1: " + objLi[1]);
            string userAddress2Id = objLi[1].Substring(0, objLi[1].Length - 3);
            string updateUserAddressQuery = "SELECT Id, Name, iq_sj_omr_01__Country__c, iq_sj_omr_01__UniqueIntegrationId__c FROM iq_sj_omr_01__OMUserAddress__c where Id = '" + userAddress2Id + "'";
            string userAddressIntegrationId = _collectSalesForce.GetValue(accessToken, updateUserAddressQuery, "iq_sj_omr_01__UniqueIntegrationId__c");
            string snowUserAddressDateUpdate = "update \"" + _sfDBName + "\".\"STATIC\".\"OMUserAddress\" set \"EffectiveDate\" = '" + DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") + "' , \"EndDate\" = '" + DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") + "'  where \"UniqueIntegrationId\" = '" + userAddressIntegrationId + "'";
            string UserAddressEndDate = _sfdbData.get_TableWise_Data(snowUserAddressDateUpdate, "EndDate", _snowConnection);

            //Running publish job and waiting for 2 mins
            PushJob();
            Thread.Sleep(150000);
            _output.WriteLine("Publish job ran successfully");


            //Userstory-OMR-2027, OMR-1318
            //Product creation and verification of competitor product update in OCEsalesforceDB
            objLi = objdict["Product"];
            _output.WriteLine("Product1 :" + objLi[0]);

            PULLAccountTest pulltest = new PULLAccountTest(_output);

            _output.WriteLine("Insertion check for all entities");
            //Product Insert check
            string SnowProductCreationcheckquery = "SELECT Id, Name, iq_sj_omr_01__CompetitorProduct__c FROM iq_sj_omr_01__OMProduct__c where ID = '" + objLi[0] + "'";
            string productName = _collectSalesForce.GetValue(accessToken, SnowProductCreationcheckquery, "Name");


            //Product entity Column mapping check
            string SnowProdColumnCheckQuery = "select \"Name\",\"CompetitorProduct\",\"Status\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMProduct\" Where \"Name\" = '" + productName + "'";
            string SalesProdColumnCheckQuery = "SELECT Name, OCE__CompetitorProduct__c, OCE__IsActive__c FROM OCE__Product__c where Name = '" + productName + "'";
            //Get the value From Snow Flake DB and Sales Force DB
            var objProdDict = pulltest.GetQueryDictionary(SnowProdColumnCheckQuery, SalesProdColumnCheckQuery);
            var objSnowDict = objProdDict.Key;
            var objSalesDict = objProdDict.Value;
            var prodBlnFlag = commonFunc.GetTableComparison(objSnowDict, objSalesDict,_output);
            Assert.True(prodBlnFlag);
            _output.WriteLine("Competitor product updated as true and client competitor product value updated as Competitor");


            //OMR-1299 -OMTerritory
            objLi = objdict["Territory"];
            _output.WriteLine("Territory :" + objLi[0]);
            string SFTerritoryCreationQuery = "SELECT Id, Name FROM iq_sj_omr_01__OMScenarioTerritory__c where Id='" + objLi[0] + "'";
            string TerritoryName = _collectSalesForce.GetValue(accessToken, SFTerritoryCreationQuery, "Name");
            string SnowTerritoryCreationquery = "select * from \"" + _sfDBName + "\".\"OUTPUT\".\"OMTerritory\" where \"Name\" = '" + TerritoryName + "'";
            string TerritoryOceSalesId = _sfdbData.get_TableWise_Data(SnowTerritoryCreationquery, "OceSalesId", _snowConnection);
            _output.WriteLine("TerritoryOceSalesId is: " + TerritoryOceSalesId);
            Assert.NotNull(TerritoryOceSalesId);
            string SFTerritoryCreationCheckQuery = "SELECT Id, Name, ParentTerritory2Id, LastModifiedDate FROM Territory2 where Id='" + TerritoryOceSalesId + "'";
            string TerritoryLastModifiedDate = _collectSalesForce.GetValue(accessToken, SFTerritoryCreationCheckQuery, "LastModifiedDate");
            Assert.Contains(DateTime.Now.ToString("MM/dd/yyyy"), TerritoryLastModifiedDate);
            string TerritorySFOcesalesId = _collectSalesForce.GetValue(accessToken, SFTerritoryCreationCheckQuery, "Id");
            Assert.Equal(TerritoryOceSalesId, TerritorySFOcesalesId);
            _output.WriteLine("Territory OCESalesid updated successfully and modification and insertion verified successfully for Territory userstory");


            //Territory creation with expired record
            objLi = objdict["Territory"];
            _output.WriteLine("Territory :" + objLi[3]);
            string Territory4CreationQuery = "SELECT Id, Name FROM iq_sj_omr_01__OMScenarioTerritory__c where Id='" + objLi[3] + "'";
            string SFTerritory4Name = _collectSalesForce.GetValue(accessToken, Territory4CreationQuery, "Name");
            string SnowFlakeTerritory4Creationquery = "select * from \"" + _sfDBName + "\".\"OUTPUT\".\"OMTerritory\" where \"Name\" = '" + SFTerritory4Name + "'";
            string Territory4OceSalesId = _sfdbData.get_TableWise_Data(SnowFlakeTerritory4Creationquery, "OceSalesId", _snowConnection);
           //_output.WriteLine("TerritoryOceSalesId is: " + Territory4OceSalesId);
            Assert.Contains("", Territory4OceSalesId);
            _output.WriteLine("Territory ocesalesid is updated null and not seen in OCE DB for the expired record");


            //OMR-956 - OMTerritoryHierarchy userstory
            objLi = objdict["Territory"];
            _output.WriteLine("Territory :" + objLi[1]);
            string SFTerritoryHierarchyCreationQuery = "SELECT Id, Name FROM iq_sj_omr_01__OMScenarioTerritory__c where Id='" + objLi[1] + "'";
            string Territory2Name = _collectSalesForce.GetValue(accessToken, SFTerritoryHierarchyCreationQuery, "Name");
            string SnowTerritoryHierarchyCreationquery = "select * from \"" + _sfDBName + "\".\"OUTPUT\".\"OMTerritory\" where \"Name\" = '" + Territory2Name + "'";
            string Territory2OceSalesId = _sfdbData.get_TableWise_Data(SnowTerritoryHierarchyCreationquery, "OceSalesId", _snowConnection);
            //_output.WriteLine("Territory2OceSalesId is: " + Territory2OceSalesId);
            Assert.NotNull(Territory2OceSalesId);
            string SFTerritory2CreationCheckQuery = "SELECT Id, Name, ParentTerritory2Id, LastModifiedDate FROM Territory2 where Id ='" + TerritoryOceSalesId + "'";
            string ParentTerritory2OceSalesId = _collectSalesForce.GetValue(accessToken, SFTerritory2CreationCheckQuery, "ParentTerritory2Id");
            Assert.Equal(Territory2OceSalesId, ParentTerritory2OceSalesId);
            _output.WriteLine("ParentTerritoryOcesales id is mapped successfully for the child territory");


            //OMR-1300-OMProductTerritory
            objLi = objdict["ProductTerritory"];
            _output.WriteLine("ProductTerritory :" + objLi[0]);
            string SFProductTerritoryCreationQuery = "SELECT Id, Name, iq_sj_omr_01__RefTerritoryName__c FROM iq_sj_omr_01__OMScenarioProductExplicit__c where Id='" + objLi[0] + "'";
            string productTerritoryName = _collectSalesForce.GetValue(accessToken, SFProductTerritoryCreationQuery, "iq_sj_omr_01__RefTerritoryName__c");


            //Column mapping check
            string snowFlakeProdTerritoryQuery = "select \"SOMProductTerritoryId\",\"SOMTerritoryId\",\"Status\" from \"" + _sfDBName + "\".\"OUTPUT\".\"OMProductTerritory\" where \"SOMTerritoryId\" = '" + productTerritoryName + "'";
            string salesforceProdTerritoryQuery = "SELECT OCE__UniqueIntegrationID__c, OCE__Territory__c, OCE__IsActive__c FROM OCE__TerritoryProductAlignmentRule__c where OCE__Territory__c = '" + productTerritoryName + "'";
            //Get the value From Snow Flake DB and Sales Force DB
            var objProdTerritoryDict = pulltest.GetQueryDictionary(snowFlakeProdTerritoryQuery, salesforceProdTerritoryQuery);
            var objProdTerritorySnowDict = objProdTerritoryDict.Key;
            var objProdTerritorySalesDict = objProdTerritoryDict.Value;
            var ProdTerritoryblnFlag = commonFunc.GetTableComparison(objProdTerritorySnowDict, objProdTerritorySalesDict,_output);
            Assert.True(ProdTerritoryblnFlag);
            _output.WriteLine("Insertion for product territory is seen succeesfully in OCEDB table");


            //OMR-1298- UserAssignment
            objLi = objdict["UserAssignment"];
            _output.WriteLine("UserAssignment :" + objLi[0]);
            string SFUserAssignmentCreationQuery = "SELECT Id, Name, iq_sj_omr_01__TerritoryUniqueIntegrationId__c FROM iq_sj_omr_01__OMScenarioUserAssignment__c where Id='" + objLi[0] + "'";
            string userAssignmentTerritoryName = _collectSalesForce.GetValue(accessToken, SFUserAssignmentCreationQuery, "iq_sj_omr_01__TerritoryUniqueIntegrationId__c");

            string SFUserAssignment1OceCheckQuery = "SELECT Id, UserId, Territory2Id, IsActive, LastModifiedDate FROM UserTerritory2Association where Territory2Id ='" + TerritoryOceSalesId + "'";
            string oceUserAssignmentStatus = _collectSalesForce.GetValue(accessToken, SFUserAssignment1OceCheckQuery, "IsActive");
            string oceUserAssignmentTerritoryId = _collectSalesForce.GetValue(accessToken, SFUserAssignment1OceCheckQuery, "Territory2Id");
            string UserAssignmentLastModifiedDate = _collectSalesForce.GetValue(accessToken, SFUserAssignment1OceCheckQuery, "LastModifiedDate");
            Assert.Equal(TerritoryOceSalesId, oceUserAssignmentTerritoryId);
            Assert.Equal("True", oceUserAssignmentStatus);
            Assert.Contains(DateTime.Now.ToString("MM/dd/yyyy"), UserAssignmentLastModifiedDate);
            _output.WriteLine("UserAssignment status update, insertion and modification check is successfull in OCE DB");

            //Verifying whether ocesales updated as Null for userAssignment which has expired record
            string SFUserAssignment1CreationQuery = "SELECT Id, Name, iq_sj_omr_01__TerritoryUniqueIntegrationId__c FROM iq_sj_omr_01__OMScenarioUserAssignment__c where Id='" + objLi[1] + "'";
            string userAssignment1TerritoryName = _collectSalesForce.GetValue(accessToken, SFUserAssignment1CreationQuery, "iq_sj_omr_01__TerritoryUniqueIntegrationId__c");
            string UserAssignmentOcesalesIdCheck = "select * from \"" + _sfDBName + "\".\"OUTPUT\".\"OMUserAssignment\" where \"SOMTerritoryId\" = '" + userAssignment1TerritoryName + "'";
            string UserAssignmentOceSalesId = _sfdbData.get_TableWise_Data(UserAssignmentOcesalesIdCheck, "OceSalesId", _snowConnection);
            //_output.WriteLine("UserAssignmentOceSalesId is: " + UserAssignmentOceSalesId);
            Assert.Contains("", UserAssignmentOceSalesId);
            _output.WriteLine("UserAssignment expired record verification is successfull");


            //Verification of userAddress
            objLi = objdict["UserAddress"];
            _output.WriteLine("userAddress1: " + objLi[0]);
            string userAddress1Id = objLi[0].Substring(0, objLi[0].Length - 3);            

            string SnowUserAddressCreationquery = "SELECT Id, Name, iq_sj_omr_01__Country__c FROM iq_sj_omr_01__OMUserAddress__c where Id = '" + userAddress1Id + "'";
            string userAddressCountry = _collectSalesForce.GetValue(accessToken, SnowUserAddressCreationquery, "iq_sj_omr_01__Country__c");

            //0MR-1277,OMR-1982- User Requirements
            objLi = objdict["User"];
            _output.WriteLine("user1: " + objLi[0]);
            string user1Id = objLi[0].Substring(0, objLi[0].Length - 3);

            string SnowUserCreationquery = "SELECT Id, Name, iq_sj_omr_01__LastName__c FROM iq_sj_omr_01__OMUser__c where Id = '" + user1Id + "'";
            string userLastName = _collectSalesForce.GetValue(accessToken, SnowUserCreationquery, "iq_sj_omr_01__LastName__c");


            //Past effective date - Verification of all fields mapped correctly in OCE SalesforceDb
            string SalesUserCheckquery = "select Id, LastName, Name, Country, Street, Phone, MobilePhone, IsActive from User where LastName = '" + userLastName + "'";
            string userName = _collectSalesForce.GetValue(accessToken, SalesUserCheckquery, "LastName");
            Assert.Equal(userLastName, userName);
           
            string status = _collectSalesForce.GetValue(accessToken, SalesUserCheckquery, "IsActive");
            Assert.Equal("True", status);
            string userStreet = _collectSalesForce.GetValue(accessToken, SalesUserCheckquery, "Street");
            Assert.Equal("Street", userStreet);
            string userPhone = _collectSalesForce.GetValue(accessToken, SalesUserCheckquery, "Phone");
            Assert.Equal("123456789", userPhone);
            string userMobilePhone = _collectSalesForce.GetValue(accessToken, SalesUserCheckquery, "MobilePhone");
            Assert.Equal("123456789", userMobilePhone);
            string userCountry = _collectSalesForce.GetValue(accessToken, SalesUserCheckquery, "Country");
            Assert.Equal(userAddressCountry, userCountry);
            _output.WriteLine("Status shown as Actv for current record , phone ,street and phone number updated with default values in OCEDB");
            _output.WriteLine("User country name is similar to country name as in the USERAddress table");

            //User entity Column mapping check
            string SnowUserColumnCheckQuery = "select \"FirstName\",\"MiddleName\",\"LastName\",\"Suffix\",\"Email\",\"UniqueIntegrationId\",\"TimeZoneSidKey\",\"LocaleSidKey\",\"LanguageLocaleKey\",\"EmailEncodingKey\",\"Status\" FROM \"" + _sfDBName + "\".\"STATIC\".\"OMUser\" Where \"LastName\" = '" + userName + "'";
            string SalesUserColumnCheckQuery = "SELECT FirstName, MiddleName, LastName, Suffix, Email, OCE__UniqueIntegrationID__c, TimeZoneSidKey, LocaleSidKey, LanguageLocaleKey, EmailEncodingKey, IsActive FROM User where LastName = '" + userName + "'";
            //Get the value From Snow Flake DB and Sales Force DB
            var objUserDict = pulltest.GetQueryDictionary(SnowUserColumnCheckQuery, SalesUserColumnCheckQuery);
            var objUserSnowDict = objUserDict.Key;
            var objUserSalesDict = objUserDict.Value;
            var userblnFlag3 = commonFunc.GetTableComparison(objUserSnowDict, objUserSalesDict,_output);
            Assert.True(userblnFlag3);


            //Verification of ocesalesid updated as null for User created with Future effective date
            _output.WriteLine("user4: " + objLi[3]);
            string user4Id = objLi[3].Substring(0, objLi[3].Length - 3);
            string SnowUser4Creationquery = "SELECT Id, Name, iq_sj_omr_01__LastName__c FROM iq_sj_omr_01__OMUser__c where Id = '" + user4Id + "'";
            string sfuser4LastName = _collectSalesForce.GetValue(accessToken, SnowUser4Creationquery, "iq_sj_omr_01__LastName__c");
            string SnowUser2Creationquery = "select * from \"" + _sfDBName + "\".\"STATIC\".\"OMUser\" where \"LastName\" = '" + sfuser4LastName + "'";
            string user4OceSalesId = _sfdbData.get_TableWise_Data(SnowUser2Creationquery, "OceSalesId", _snowConnection);
            string user4LastName = _sfdbData.get_TableWise_Data(SnowUser2Creationquery, "LastName", _snowConnection);
            _output.WriteLine("User4Ocesalesid is: " + user4OceSalesId);
            Assert.Equal("", user4OceSalesId);
            _output.WriteLine("User verification for expired record is successfull");


            //Update the product status to Inactive and competitor product as false
            //Fetching the user2ocesalesid for verification Userassignment2 testcase for moving territory to another territory
            /*objLi = objdict["User"];
            _output.WriteLine("user2: " + objLi[2]);
            string user2Id = objLi[2].Substring(0, objLi[2].Length - 3);
            string SnowUser2OceCreationquery = "select * from \"TITGPREPROD_PROD_3072A2D6B2D54BA0A3D8E940E9D0040F_DB\".\"STATIC\".\"OMUser\" where \"Name\" = '" + user2Id + "'";
            string user2OceSalesId = _sfdbData.get_TableWise_Data(SnowUser2OceCreationquery, "OceSalesId");
            _output.WriteLine("OceSalesId is: " + user2OceSalesId);*/

            //Updating all the records for the verification update check in OCESalesforcedb
            string SnowUpdateProductquery = "update \"" + _sfDBName + "\".\"STATIC\".\"OMProduct\" set \"Status\" = 'INAC', \"CompetitorProduct\" = 'False' where \"Name\" = '" + productName + "'";
            string productStatus = _sfdbData.get_TableWise_Data(SnowUpdateProductquery, "Status", _snowConnection);

            string updateProductTerritoryquery = "update \"" + _sfDBName + "\".\"OUTPUT\".\"OMProductTerritory\" set \"Status\" = 'INAC' where \"SOMTerritoryId\" = '" + productTerritoryName + "'";
            string productTerritoryStatus = _sfdbData.get_TableWise_Data(updateProductTerritoryquery, "Status", _snowConnection);


            //Update UserAssignment on moving from T1 to T2
            objLi = objdict["UserAssignment"];
            _output.WriteLine("UserAssignment :" + objLi[2]);
            string SFUser3AssignmentCreationQuery = "SELECT Id, Name, iq_sj_omr_01__TerritoryUniqueIntegrationId__c FROM iq_sj_omr_01__OMScenarioUserAssignment__c where Id='" + objLi[2] + "'";
            string user3AssignmentTerritoryName = _collectSalesForce.GetValue(accessToken, SFUser3AssignmentCreationQuery, "iq_sj_omr_01__TerritoryUniqueIntegrationId__c");
            string UserAssignmentMoveTerritory = "update \"" + _sfDBName + "\".\"OUTPUT\".\"OMUserAssignment\" set \"SOMTerritoryId\" = '" + userAssignmentTerritoryName + "' where \"SOMTerritoryId\" = '" + user3AssignmentTerritoryName + "'";
            string User3AssignmentName = _sfdbData.get_TableWise_Data(UserAssignmentMoveTerritory, "SOMTerritoryId", _snowConnection);

            //Updating the records to InActive and updating firstname field for field verification check in OCESalesforceDB
            string SnowUpdateUserquery = "update \"" + _sfDBName + "\".\"STATIC\".\"OMUser\" set \"Status\" = 'INAC', \"FirstName\" = 'User1' where \"LastName\" = '" + userLastName + "'";
            string userStatus = _sfdbData.get_TableWise_Data(SnowUpdateUserquery, "Status", _snowConnection);

            //Updating the records to Active and enddate to today date
            string SnowUpdateUser4query = "update \"" + _sfDBName + "\".\"STATIC\".\"OMUser\" set \"EffectiveDate\" = '" + DateTime.Now.ToString("yyyy-MM-dd") + "' where \"LastName\" = '" + sfuser4LastName + "'";
            string user4Status = _sfdbData.get_TableWise_Data(SnowUpdateUser4query, "Status", _snowConnection);

            //Running the publish job for the verification of updated entities in OCESalesDB
            PushJob();
            Thread.Sleep(150000);


            _output.WriteLine("Updates check for all entities in OCE SalesforceDB");
            //updated competitor product value as false and clinet competitor value updates as Client and status as false
            string SfProductUpdatequery = "select Id, Name, OCE__ClientCompetitorProduct__c, OCE__CompetitorProduct__c, OCE__EndDate__c, OCE__UniqueIntegrationID__c, OCE__IsActive__c FROM OCE__Product__c where Name = '" + productName + "'";
            string UpdatedclientProductstatus = _collectSalesForce.GetValue(accessToken, SfProductUpdatequery, "OCE__ClientCompetitorProduct__c");
            Assert.Equal("Client", UpdatedclientProductstatus);
            string UpdatedcompetitorProductstatus = _collectSalesForce.GetValue(accessToken, SfProductUpdatequery, "OCE__CompetitorProduct__c");
            Assert.Equal("False", UpdatedcompetitorProductstatus);
            string UpdatedProductstatus = _collectSalesForce.GetValue(accessToken, SfProductUpdatequery, "OCE__IsActive__c");
            Assert.Equal("False", UpdatedProductstatus);
            _output.WriteLine("Competitor product updated as False and client competitor product changed to client on running publish omr job");


            //Update OMProdutterritoryrecord check - Verification of status
            string SFProductTerritoryOceUpdateQuery = "SELECT Id, Name, OCE__Territory__c, OCE__IsActive__c FROM OCE__TerritoryProductAlignmentRule__c where OCE__Territory__c='" + productTerritoryName + "'";
            string oceProductTerritoryUpdateStatus = _collectSalesForce.GetValue(accessToken, SFProductTerritoryOceUpdateQuery, "OCE__IsActive__c");
            Assert.Equal("False", oceProductTerritoryUpdateStatus);
            _output.WriteLine("Record status is false on updating the record from OMProductTerritory table");

            //Verification of moved territory is updated correctly in OCE SalesforceDb
            //Fetching the user2ocesalesid for verification Userassignment2 testcase for moving territory to another territory
            objLi = objdict["User"];
            _output.WriteLine("user3: " + objLi[2]);
            string user3Id = objLi[2].Substring(0, objLi[2].Length - 3);


            string SnowUser3Creationquery = "SELECT Id, Name, iq_sj_omr_01__LastName__c FROM iq_sj_omr_01__OMUser__c where Id = '" + user3Id + "'";
            string sfuser3LastName = _collectSalesForce.GetValue(accessToken, SnowUser3Creationquery, "iq_sj_omr_01__LastName__c");

            string SnowUser2OceCreationquery = "select * from \"" + _sfDBName + "\".\"STATIC\".\"OMUser\" where \"LastName\" = '" + sfuser3LastName + "'";
            string user3OceSalesId = _sfdbData.get_TableWise_Data(SnowUser2OceCreationquery, "OceSalesId", _snowConnection);
            _output.WriteLine("User3OceSalesId is: " + user3OceSalesId);


            string SFUserAssignmentupdatedQuery = "SELECT Id, UserId, Territory2Id, IsActive, LastModifiedDate FROM UserTerritory2Association where Territory2Id ='" + TerritoryOceSalesId + "' and UserId = '" + user3OceSalesId + "'";
            string SFUserAssignmentTerritory2Id = _collectSalesForce.GetValue(accessToken, SFUserAssignmentupdatedQuery, "Territory2Id");
            Assert.Equal(TerritoryOceSalesId, SFUserAssignmentTerritory2Id);


            //User updates- Verification of updated field and status mapped correctly in OCE SalesforceDB 
            string SalesUpdateCheckquery = "select Id, OCE__UniqueIntegrationID__c, Email, Country, FirstName, LastName, Name, Suffix, TimeZoneSidKey, LocaleSidKey, CreatedById, CreatedDate, LastModifiedDate, IsActive from User where LastName = '" + userLastName + "'";
            string userFirstName = _collectSalesForce.GetValue(accessToken, SalesUpdateCheckquery, "FirstName");
            Assert.Equal("User1", userFirstName);
            status = _collectSalesForce.GetValue(accessToken, SalesUpdateCheckquery, "IsActive");
            Assert.Equal("False", status);
            _output.WriteLine("Status shown as INAC for current updated record on running publish omr job");

            //Verification status updated as True on updating the record from future date to effective date
            string SfU2query = "select Id, OCE__UniqueIntegrationID__c, Email, Country, FirstName, LastName, Name, Suffix, TimeZoneSidKey, LocaleSidKey, CreatedById, CreatedDate, LastModifiedDate, IsActive from User where LastName = '" + user4LastName + "'";
            _output.WriteLine("Query is: " + SfU2query);
            userName = _collectSalesForce.GetValue(accessToken, SfU2query, "LastName");
            Assert.Equal(user4LastName, userName);
            status = _collectSalesForce.GetValue(accessToken, SfU2query, "IsActive");
            Assert.Equal("True", status);

            string userAddress2Country = _collectSalesForce.GetValue(accessToken, SfU2query, "Country");
            Assert.Equal("", userAddress2Country);
            _output.WriteLine("User country field value is null for end dated useraddress record");
            _output.WriteLine("Status shown as ACTV and record seen in OCEDB on updating effectivedate to current date from future date");


        }


    }
}
