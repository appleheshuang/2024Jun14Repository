using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioAccountExplicit
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string OMAccountId__c;
        public string AccountUniqueIntegrationId__c;
        public string SOMTerritoryId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string Purpose__c;
        public string ExternalId1__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string SOMAccountExplicitId__c;
        public string RefAccountName__c;
        public string RefAccountAccountType__c;
        public string RefAccountType__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string RefSalesForceName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public bool AutoCreateAccountOwner__c;

        public override string ToString()
        {
            return string.Format("{0}ing Account {1} Explicit to Territory {2}", Action__c, AccountUniqueIntegrationId__c, TerritoryUniqueIntegrationId__c);
        }
    };
}
