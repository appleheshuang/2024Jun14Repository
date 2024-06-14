using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ScenarioProductAccTerrExplicit
    {
        [JsonIgnore]
        public string Id;
        public string OMScenarioId__c;
        public string AccountUniqueIntegrationId__c;
        public string Action__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public string ExternalId1__c;
        public string LookupDate__c;
        public string OMAccountId__c;
        public string ProductUniqueIntegrationId__c;
        public string RefAccountAccountType__c;
        public string RefAccountCustomerType__c;
        public string RefAccountName__c;
        public string RefAccountType__c;
        public string RefEffectiveDate__c;
        public string RefEndDate__c;
        public string RefProductName__c;
        public string RefProductType__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string SOMProductAccTerrExplicitId__c;
        public string SOMProductId__c;
        public string SOMTerritoryId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string UniqueIntegrationId__c;


        public override string ToString()
        {
            return string.Format("{0}ing Product {1} Account {2} Explicit to Territory {3}", Action__c, ProductUniqueIntegrationId__c, AccountUniqueIntegrationId__c, TerritoryUniqueIntegrationId__c);
        }
    }
}
