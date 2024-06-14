using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioProduct
    {
        [JsonIgnore]
        public string Id;
        public string Name;
        public string Action__c;
        public bool CascadeChildRecords__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public string ImpactedRegions__c;
        public string OMScenarioId__c;
        public string PriorEffectiveDate__c;
        public string PriorEndDate__c;
        public string RefProductName__c;
        public string RefProductType__c;
        public string RefRegionName__c;
        public string RegionUniqueIntegrationId__c;
        public string SOMProductId__c;
        public string SOMRegionId__c;
        public string UniqueIntegrationId__c;

        public override string ToString()
        {
            return string.Format("{0}ing SalesForce Name {1}", Action__c, Name);
        }
    };
}
