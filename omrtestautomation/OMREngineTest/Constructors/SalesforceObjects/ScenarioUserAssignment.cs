using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioUserAssignment
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMTerritoryId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string SOMUserId__c;
        public string UserUniqueIntegrationId__c;
        public string AssignmentType__c;
        public string ExternalId1__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string SOMUserAssignmentId__c;
        public string RefUserName__c;
        public string RefUserType__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string RefSalesForceName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public string SOMUserTerritoryTitleId__c;
        public string SOMFieldTitleId__c;
        public string TitleDescription__c;
        public string FieldTitleUniqueIntegrationId__c;

        public override string ToString()
        {
            return string.Format("{0}ing User {1} Assignment to Territory {2}", Action__c, UserUniqueIntegrationId__c, TerritoryUniqueIntegrationId__c);
        }
    };
}
