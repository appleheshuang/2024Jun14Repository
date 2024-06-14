using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioAccountProductRule
    {
        [JsonIgnore]
        public string Id;
        public string SOMAccountProductRuleId__c;
        public string SalesForceUniqueIntegrationId__c;
        public string SOMSalesForceId__c;
        public string RefSalesForceName__c;
        public string Name;
        public string Description__c;
        public string RuleData__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public string UniqueIntegrationId__c;
        public string Action__c;
        public string OMScenarioId__c;
        public string ExternalId1__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing Account Rule {1} ({2}) for SalesForce {3}", Action__c, UniqueIntegrationId__c, Name, SalesForceUniqueIntegrationId__c);
        }
    };
}
