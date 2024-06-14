using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ScenarioModelTerritory
    {
        [JsonIgnore]
        public string Id;
        public string Name;
        public string OMScenarioId__c;
        public string OMScenarioModelId__c;
        public string SOMTerritoryId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefSalesForceName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string TerritoryUniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefTerritoryName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefTerritoryType__c;

        public override string ToString()
        {
            return string.Format("Processing Territory Model {0}", Name);
        }
    };
}