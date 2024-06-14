using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ScenarioModel
    {
        [JsonIgnore]
        public string Id;
        public string OMScenarioId__c;
        public string Name;
        public string SOMSalesForceId__c;
        public string MetricField__c;
        public float MetricMinimum__c;
        public float MetricMaximum__c;
        public float DrivingDistanceMaximum__c;
        public float DrivingTimeMaximum__c;
        public string DrivingUnit__c;
        public string DistanceContext__c;
        public bool RestrictToHierarchy__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ModelStatus__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AppliedDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LastOptimizedDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefSalesForceName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SalesForceUniqueIntegrationId__c;

        public override string ToString()
        {
            return string.Format("Processing Model {1}", Name);
        }
    };
}