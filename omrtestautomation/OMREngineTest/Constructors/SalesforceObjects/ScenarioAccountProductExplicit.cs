using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioAccountProductExplicit
    {
        [JsonIgnore]
        public string Id;
        public string SOMAccountProductExplicitId__c;
        public string AccountUniqueIntegrationId__c;
        public string OMAccountId__c;
        public string RefAccountName__c;
        public string RefAccountAccountType__c;
        public string RefAccountType__c;
        public string ProductUniqueIntegrationId__c;
        public string SOMProductId__c;
        public string RefProductName__c;
        public string RefProductType__c;
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
            return string.Format("{0}ing Product {1} Explicit to Account {2}", Action__c, ProductUniqueIntegrationId__c, AccountUniqueIntegrationId__c);
        }
    };
}
