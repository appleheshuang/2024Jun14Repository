using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ScenarioProductExplicit
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMProductId__c;
        public string ProductUniqueIntegrationId__c;
        public string SOMTerritoryId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string ProductAlignmentType__c;
        public string ExternalId1__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string SOMProductExplicitId__c;
        public string RefProductName__c;
        public string RefProductType__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string RefSalesForceName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefEffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefEndDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing Product {1} Explicit to Territory {2}", Action__c, ProductUniqueIntegrationId__c, TerritoryUniqueIntegrationId__c);
        }
    }
}
