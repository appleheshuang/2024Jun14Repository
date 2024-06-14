using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioEngagementPlan
    {
        [JsonIgnore]
        public string Id;
        public string Name;
        public string Description__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMEngagementPlanId__c;
        public string SOMRegionId__c;
        public string SOMSalesForceId__c;
        public string SalesForceUniqueIntegrationId__c;
        public string RefSalesForceName__c;
        public string WorkingDays__c;
        public string TargetCallRate__c;
        public string MinimumCallRate__c;
        public string MaximumCallRate__c;
        public string OMEngagementChannel__c;
        public string OMEngagementSegment__c;
        public string ExternalId1__c;
        public string PlanType__c;
        public bool ImportEngagementPlan__c;
        public string RegionUniqueIntegrationId__c;
        public string RefRegionName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public override string ToString()
        {
            return string.Format("{0}ing EngagementPlan Name {1}", Action__c, Name, UniqueIntegrationId__c);
        }
    }
}
