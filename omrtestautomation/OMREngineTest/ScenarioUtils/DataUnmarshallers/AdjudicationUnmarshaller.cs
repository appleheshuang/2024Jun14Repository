using MainFramework.GlobalUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMREngineTest.Constructors;
using OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;

namespace OMREngineTest.ScenarioUtils
{
    public enum adj_user_type { rep, dm, ho, dflt };

    class AdjudicationUnmarshaller : OptimizerUnmarshaller
    {
        private Dictionary<adj_user_type, Tuple<string,string>> adj_users;
        private SalesforceApi sf_adjuser;

        public AdjudicationUnmarshaller(ScenarioContext scenarioContext, string tconfig, string testid, string testdir=null) : base(scenarioContext, tconfig, testid, datapath: testdir)
        {
            string username, password, userid, username_key, userid_key;
            adj_users = new Dictionary<adj_user_type, Tuple<string, string>>();
            foreach (adj_user_type this_type in Enum.GetValues(typeof(adj_user_type))) {
                username_key = this_type.ToString() + "_username";
                userid_key = this_type.ToString() + "_userid";
                username = adjUserName(this_type);
                password = adjPassword(this_type);
                // If the user is not found, then show a warning and default back to the main sf user
                try
                {
                    userid = sf.GetSFUserId(username);
                    adj_users.Add(this_type, new Tuple<string,string>(username,password));
                }
                catch
                {
                    Console.WriteLine("Warning: "+ this_type.ToString() + " adjudication user not found '" + username + "'.\n Defaulting to '" + tenant.Config.orgUserName + "'");
                    username = tenant.Config.orgUserName;
                    password = tenant.Config.orgPassword;
                    userid = sf.GetSFUserId(username);
                    adj_users.Add(this_type, new Tuple<string, string>(username, password));
                }
                dataMap[userid_key] = userid;
                dataMap[username_key] = username;
            }
            scenarioContext["dynamicDataMap"] = dataMap;
            sf_adjuser = new SalesforceApi(tenant.Config);
        }

        private string adjUserName(adj_user_type type = adj_user_type.dflt) {
            switch (type)
            {
                case adj_user_type.rep: return tenant.Config.adjRepUser ?? "rep@" + domain + ".com"; 
                case adj_user_type.dm: return tenant.Config.adjDmUser ?? "dm@" + domain + ".com"; 
                case adj_user_type.ho: return tenant.Config.adjHoUser ?? "ho@" + domain + ".com"; 
                default: return tenant.Config.orgUserName; 
            }
        }
        private string adjPassword(adj_user_type type = adj_user_type.dflt)
        {
            switch (type)
            {
                case adj_user_type.rep: return tenant.Config.adjRepPassword ?? tenant.Config.orgPassword;
                case adj_user_type.dm: return tenant.Config.adjDmPassword ?? tenant.Config.orgPassword;
                case adj_user_type.ho: return tenant.Config.adjHoPassword ?? tenant.Config.orgPassword;
                default: return tenant.Config.orgPassword;
            }
        }
        
        public string AssumeIdentity(adj_user_type type)
        {
            return sf_adjuser.AssumeUserIdentity(adj_users[type].Item1, adj_users[type].Item2);
        }

        /// <summary>
        /// DummyScenarioForCurrentAdjudication
        /// Posts a new scenario to sfdc and returns a scenario object with the details
        /// Used when processing an adjudication that is not based off a scenario 
        /// </summary>
        /// <param name="adjudication"></param>
        /// <returns>scenario object with the details of the dummy scenario to be committed</returns>
        public Scenario DummyScenarioForCurrentAdjudication(Adjudication adjudication)
        {
            Scenario sc = new Scenario();
            sc.Description__c = "Auto Created for " + adjudication.Name;
            sc.Name = "Auto Created for " + adjudication.Name + "-" + testId;
            sc.EffectiveDate__c = adjudication.AdjudicationEffectivedate__c;
            sc.EndDate__c = adjudication.AdjudicationEndDate__c;
            sc.ScenarioStatus__c = "PENDG";
            sc.UniqueIntegrationId__c = "ADJC-" + testId;
            sc.Type__c = "ADJC";
            sc.OMAdjudicationId__c = adjudication.Id;
            sc.OMRegionId__c = adjudication.OMRegionId__c;
            sc.Id = sf.Post_Object("OMScenario__c", sc);
            return sc;
        }

        public Adjudication PostAdjudicationObjectsFromFile(string filePath, bool isScenarioBased)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;

                SetBaseDataMap(jsonObj); 
                string adjudicationUID = chelp.RandomString(5) + Convert.ToString(new Random().Next(10, 1000)) + DateTime.Now.ToString("yyyyMMdd");

                //Set adjudication object
                Adjudication adjudication = jsonObj["Adjudication"]?.ToObject<Adjudication>();
                SetObjectPropertiesWithBaseData(adjudication, baseDataMap, dataMap);
                SetTestUniqueIntegrationId(adjudication, adjudicationUID);
                adjudication.setFieldsToUniqueIntegrationID(scenarioname, adjudicationUID);
                string SOMRegionId = adjudication.OMRegionId__c;
                adjudication.OMRegionId__c = sf.LookupValue("OMRegion__c", "Id", "SOMRegionId__c", SOMRegionId);

                if (isScenarioBased)
                {
                    Scenario scenario = scenarioContext.Get<Scenario>("Scenario");
                    adjudication.OMScenarioId__c = scenario.Id;
                    adjudication.AdjudicationEffectivedate__c = scenario.EffectiveDate__c;
                    adjudication.AdjudicationEndDate__c = scenario.EndDate__c;
                    adjudication.RefScenarioEffectiveDate__c = scenario.EffectiveDate__c;
                    adjudication.RefScenarioEndDate__c = scenario.EndDate__c;
                    adjudication.RefScenarioUniqueIntegrationId__c = scenario.UniqueIntegrationId__c;
                    adjudication.setType();
                    adjudication.Id = sf_adjuser.Post_Object("OMAdjudication__c", adjudication);

                    scenario.OMAdjudicationId__c = adjudication.Id;
                    sf.Patch_Object("OMScenario__c", scenario, scenario.Id);
                    
                } else
                {
                    adjudication.setType();
                    adjudication.Id = sf.Post_Object("OMAdjudication__c", adjudication);
                }

                //Create AdjudicationRegion
                AdjudicationRegion ar = new AdjudicationRegion();
                ar.OMAdjudicationId__c = adjudication.Id;
                ar.SOMRegionId__c = SOMRegionId;
                sf.Post_Object_NoId("OMAdjudicationRegion__c", ar);

                //adjudication.OMRegionId__c = (adjudication.OMRegionId__c == "") ? baseDataMap["som_region_id"] : adjudication.OMRegionId__c; //R505
                //UpdateDataMap(adjudication, dataMap);
                AddToDataMapSingleObject(adjudication, "adjudicationCount");


                return adjudication;
            }
            return null;
        }


        public List<AdjudicationLevel> PostAdjudicationLevelsFromFile(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                baseDataMap = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonObj["BaseData"].ToString());

                //Set adjudication object
                List<AdjudicationLevel> adjudicationLevels = jsonObj["AdjudicationLevel"]?.ToObject<List<AdjudicationLevel>>();
                foreach (AdjudicationLevel adjudicationLevel in adjudicationLevels)
                {
                    SetObjectPropertiesWithBaseData(adjudicationLevel, baseDataMap, dataMap);
                    adjudicationLevel.Id = sf_adjuser.Post_Object("OMAdjudicationLevel__c", adjudicationLevel);
                    AddToDataMapSingleObject(adjudicationLevel, "adjudicationLevelsCount");
                }
                return adjudicationLevels;
            }
            return null;

        }

        public AdjudicationGuardRail PostAdjudicationGuardRailsFromFile(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                baseDataMap = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonObj["BaseData"].ToString());

                //Set adjudication object
                AdjudicationGuardRail adjudicationGuardRail = jsonObj["AdjudicationGuardRail"]?.ToObject<AdjudicationGuardRail>();
                {
                    SetObjectPropertiesWithBaseData(adjudicationGuardRail, baseDataMap, dataMap);
                    adjudicationGuardRail.Id = sf_adjuser.Post_Object("OMAdjudicationGuardRail__c", adjudicationGuardRail);
                    AddToDataMapSingleObject(adjudicationGuardRail, "adjudicationGuardRailCount");
                }
                return adjudicationGuardRail;
            }
            return null;
        }

        public List<AdjudicationChangeRequestAccount> PostAdjudicationAccountChangeRequestsFromFilePath(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                baseDataMap = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonObj["BaseData"].ToString());

                //Set adjudication object
                List<AdjudicationChangeRequestAccount> adjudicationChangeRequestAccounts = jsonObj["AdjudicationChangeRequestAccount"]?.ToObject<List<AdjudicationChangeRequestAccount>>() ?? new List<AdjudicationChangeRequestAccount>();
                foreach (AdjudicationChangeRequestAccount adjudicationChangeRequestAccount in adjudicationChangeRequestAccounts)
                {
                    SetObjectPropertiesWithBaseData(adjudicationChangeRequestAccount, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(adjudicationChangeRequestAccount);
                    adjudicationChangeRequestAccount.Id = sf_adjuser.Post_Object("OMChangeRequestAccount__c", adjudicationChangeRequestAccount);
                    AddToDataMapSingleObject(adjudicationChangeRequestAccount, "adjudicationChangeRequestAccountCount");
                    //UpdateDataMap(adjudicationChangeRequestAccount, dataMap);
                }
                return adjudicationChangeRequestAccounts;
            }
            return null;
        }

        public List<AdjudicationChangeRequestGeography> PostAdjudicationGeographyChangeRequestsFromFilePath(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                JObject jsonObj = testdata.Item2;
                baseDataMap = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonObj["BaseData"].ToString());

                //Set adjudication object
                List<AdjudicationChangeRequestGeography> adjudicationCRs = jsonObj["AdjudicationChangeRequestGeography"]?.ToObject<List<AdjudicationChangeRequestGeography>>() ?? new List<AdjudicationChangeRequestGeography>();
                foreach (AdjudicationChangeRequestGeography adjudicationCR in adjudicationCRs)
                {
                    SetObjectPropertiesWithBaseData(adjudicationCR, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(adjudicationCR);
                    adjudicationCR.OMGeographyId__c = sf.LookupValue("OMGeography__c", "Id", "SOMGeographyId__c", adjudicationCR.SOMGeographyId__c);
                    adjudicationCR.Id = sf_adjuser.Post_Object("OMChangeRequestGeography__c", adjudicationCR);
                    AddToDataMapSingleObject(adjudicationCR, "adjudicationChangeRequestGeographyCount");
                }
                return adjudicationCRs;
            }
            return null;
        }

        //public List<AdjudicationChangeRequestGeography> GetAdjudicationGeographyChangeRequestsFromFilePath(string filePath)
        //{

        //}
        public List<AdjudicationTerritory> PostAdjudicationTerritoriesFromFile(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                SetBaseDataMap(jsonObj);

                //Set adjudication object
                List<AdjudicationTerritory> adjudicationTerritories = jsonObj["AdjudicationTerritory"]?.ToObject<List<AdjudicationTerritory>>();
                foreach (AdjudicationTerritory adjudicationTerritory in adjudicationTerritories)
                {
                    SetObjectPropertiesWithBaseData(adjudicationTerritory, baseDataMap, dataMap);
                    adjudicationTerritory.Id = sf_adjuser.Post_Object("OMAdjudicationTerritory__c", adjudicationTerritory);
                    //UpdateDataMap(adjudicationTerritory, dataMap);
                    AddToDataMapSingleObject(adjudicationTerritory, "adjudicationTerritoryCount");
                }
                return adjudicationTerritories;
            }
            return null;
        }

        public AdjudicationApiRequestBody GetRequestBodyFromFile(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;

                var adjudicationApiRequestBody = jsonObj["AdjudicationRequest"]?.ToObject<AdjudicationApiRequestBody>();
                SetObjectPropertiesWithBaseData(adjudicationApiRequestBody, baseDataMap, dataMap);

                for (int i = 0; i < adjudicationApiRequestBody?.Territories?.Count; i++)
                {
                    adjudicationApiRequestBody.Territories[i] = SetStringValueWithBaseData(adjudicationApiRequestBody.Territories[i], baseDataMap, dataMap);
                }

                for (int i = 0; i < adjudicationApiRequestBody?.AccessDateFields?.Count; i++)
                {
                    adjudicationApiRequestBody.AccessDateFields[i] = SetStringValueWithBaseData(adjudicationApiRequestBody.AccessDateFields[i], baseDataMap, dataMap);
                }

                for (int i = 0; i < adjudicationApiRequestBody?.AccessKeyFields?.Count; i++)
                {
                    adjudicationApiRequestBody.AccessKeyFields[i] = SetStringValueWithBaseData(adjudicationApiRequestBody.AccessKeyFields[i], baseDataMap, dataMap);
                }


                foreach (GeographyChangeRequest geographyChangeRequest in adjudicationApiRequestBody.GeographyChangeRequests ?? Enumerable.Empty<GeographyChangeRequest>())
                {
                    SetObjectPropertiesWithBaseData(geographyChangeRequest, baseDataMap, dataMap);
                }
                foreach (AccountChangeRequest accountChangeRequest in adjudicationApiRequestBody.AccountChangeRequests ?? Enumerable.Empty<AccountChangeRequest>())
                {
                    SetObjectPropertiesWithBaseData(accountChangeRequest, baseDataMap, dataMap);
                }
                return adjudicationApiRequestBody;
            }
            return null;
        }

        public void ReleaseAdjudication(Adjudication adjudication)
        {
            adjudication.AdjudicationStatus__c = "RELEASING";
            sf_adjuser.Patch_Object("OMAdjudication__c", adjudication, adjudication.Id);
        }

        public void PatchApprovalStatusAccountCR(AdjudicationChangeRequestAccount accountCR, string newStatus)
        {
            accountCR.ApprovalStatus__c = newStatus;
            sf_adjuser.Patch_Object("OMChangeRequestAccount__c", accountCR, accountCR.Id);
        }

        public void PatchApprovalStatusGeoCR(AdjudicationChangeRequestGeography geoCR, string newStatus)
        {
            geoCR.ApprovalStatus__c = newStatus;
            sf_adjuser.Patch_Object("OMChangeRequestGeography__c", geoCR, geoCR.Id);
        }

        public void SubmitChangeRequestAccount(AdjudicationChangeRequestAccount accountCR, string approvalStatus = "PEND")
        {
            accountCR.ApprovalStatus__c = approvalStatus;
            accountCR.SubmitStatus__c = "SUBM";
            sf_adjuser.Patch_Object("OMChangeRequestAccount__c", accountCR, accountCR.Id);
        }
        public void SubmitChangeRequestGeography(AdjudicationChangeRequestGeography adjCR, string approvalStatus = "PEND")
        {
            adjCR.ApprovalStatus__c = approvalStatus;
            adjCR.SubmitStatus__c = "SUBM";
            sf_adjuser.Patch_Object("OMChangeRequestGeography__c", adjCR, adjCR.Id);
        }

        public void PatchSimulateStatusAccountCR(AdjudicationChangeRequestAccount adjCR, string newStatus, string role)
        {
            switch (role)
            {
                case "APPR": adjCR.SimulationStatusApprover__c = newStatus; break;
                case "REQ": adjCR.SimulationStatusRep__c = newStatus; break;
            }
            sf_adjuser.Patch_Object("OMChangeRequestAccount__c", adjCR, adjCR.Id);
        }

        public void PatchSimulateStatusGeoCR(AdjudicationChangeRequestGeography adjCR, string newStatus, string role)
        {
            switch (role)
            {
                case "APPR": adjCR.SimulationStatusApprover__c = newStatus; break;
                case "REQ": adjCR.SimulationStatusRep__c = newStatus; break;
            }
            sf_adjuser.Patch_Object("OMChangeRequestGeography__c", adjCR, adjCR.Id);
        }

        public void AdjudicationApproveGeoChangeRequest(AdjudicationChangeRequestGeography geoCR)
        {
            geoCR.ApprovalStatus__c = "APPR";
            sf_adjuser.Patch_Object("OMChangeRequestGeography__c", geoCR, geoCR.Id);
        }

        public void AdjudicationApproveAccountChangeRequest(AdjudicationChangeRequestAccount accCr)
        {
            accCr.ApprovalStatus__c = "APPR";
            sf_adjuser.Patch_Object("OMChangeRequestAccount__c", accCr, accCr.Id);
        }

        public void CloseAdjudication(Adjudication adjudication)
        {
            adjudication.AdjudicationStatus__c = "CLOSED";
            sf_adjuser.Patch_Object("OMAdjudication__c", adjudication, adjudication.Id);
        }
    }
}
