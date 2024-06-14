using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ScenarioProductSalesForce
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMProductId__c;
        public string ProductUniqueIntegrationId__c;
        public string SOMSalesForceId__c;
        public string SalesForceUniqueIntegrationId__c;
        public string ProductAlignmentType__c;
        public string ExternalId1__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string SOMProductSalesForceId__c;
        public string RefProductName__c;
        public string RefProductType__c;
        public string RefSalesForceName__c;
        public string RefSalesForceType__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing Product {1} to SalesForce {2}", Action__c, ProductUniqueIntegrationId__c, SalesForceUniqueIntegrationId__c);
        }
    }
}
