using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioEngagementResult
    {
        [JsonIgnore]
        public string Id;
        public string OMScenarioId__c;
        public string SOMEngagementResultId__c;
        public string OMAccountId__c;
        public string AccountUniqueIntegrationId__c;
        public string SOMTerritoryId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string SOMEngagementPlanId__c;
        public string EngagementPlanUniqueIntegrationId__c;
        public string SOMEngagementSegmentId__c;
        public string EngagementSegmentName__c;
        public string OMScenarioEngagementPlanId__c;
        public string Targets__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public override string ToString()
        {
            return string.Format("Loading EngagementResult {0}", UniqueIntegrationId__c);
        }
    }
}
