using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ScenarioTerritoryHierarchy
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMParentTerritoryId__c;
        public string ParentTerritoryUniqueIntegrationId__c;
        public string SOMChildTerritoryId__c;
        public string ChildTerritoryUniqueIntegrationId__c;
        public string RelationType__c;
        public string ExternalId1__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string SOMTerritoryHierarchyId__c;
        public string RefParentTerritoryName__c;
        public string RefParentTerritoryType__c;
        public string RefChildTerritoryName__c;
        public string RefChildTerritoryType__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing Territory {1} as child to parent Territory {2}", Action__c, ChildTerritoryUniqueIntegrationId__c, ParentTerritoryUniqueIntegrationId__c);
        }
    };
}
