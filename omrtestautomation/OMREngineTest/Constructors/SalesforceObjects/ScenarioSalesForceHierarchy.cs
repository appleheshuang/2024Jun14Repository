using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ScenarioSalesForceHierarchy
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMParentSalesForceId__c;
        public string ParentSalesForceUniqueIntegrationId__c;
        public string SOMChildSalesForceId__c;
        public string ChildSalesForceUniqueIntegrationId__c;
        public string RelationType__c;
        public string ExternalId1__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string SOMSalesForceHierarchyId__c;
        public string RefParentSalesForceName__c;
        public string RefParentSalesForceType__c;
        public string RefChildSalesForceName__c;
        public string RefChildSalesForceType__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing SalesForce {1} as child to parent SalesForce {2}", Action__c, ChildSalesForceUniqueIntegrationId__c, ParentSalesForceUniqueIntegrationId__c);
        }
    };
}
