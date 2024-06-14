using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioSalesForce
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMSalesForceId__c;
        public string Name;
        public string Type__c;
        public string Description__c;
        public string SOMRegionId__c;
        public string RegionUniqueIntegrationId__c;
        public string ExternalId1__c;
        public string ExternalId2__c;
        public string ExternalId3__c;
        public string ExternalId4__c;
        public string ExternalId5__c;
        public string UniqueIntegrationId__c;
        public bool Cascade__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public bool AlignmentLimitedToRegion__c;
        public string RefRegionName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing SalesForce Name {1}", Action__c, Name);
        }
    };
}
