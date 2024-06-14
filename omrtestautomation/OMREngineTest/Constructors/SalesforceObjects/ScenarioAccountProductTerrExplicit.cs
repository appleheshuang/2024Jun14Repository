using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioAccountProductTerrExplicit
    {
        [JsonIgnore]
        public string Id;
        public string SOMAccountProductTerritoryExplicitId__c;
        public string AccountUniqueIntegrationId__c;
        public string OMAccountId__c;
        public string RefAccountName__c;
        public string RefAccountAccountType__c;
        public string RefAccountType__c;
        public string ProductUniqueIntegrationId__c;
        public string SOMProductId__c;
        public string RefProductName__c;
        public string RefProductType__c;
        public string TerritoryUniqueIntegrationId__c;
        public string SOMTerritoryId__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public string UniqueIntegrationId__c;
        public string Action__c;
        public string OMScenarioId__c;
        public string ExternalId1__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing Product {1} Explicit to Territory {2} for Account {3}", Action__c, ProductUniqueIntegrationId__c, TerritoryUniqueIntegrationId__c, AccountUniqueIntegrationId__c);
        }
    };
}
