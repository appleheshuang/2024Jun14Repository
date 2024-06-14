using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioChangeRequest
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string OMAccountId__c;
        public string AccountUniqueIntegrationId__c;
        public string RefAccountName__c;
        public string SOMTerritoryId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string RefTerritoryName__c;
        public string OMScenarioEngagementPlanId__c;
        public string SOMEngagementSegmentId__c;
        public string RefSegmentName__c;
        public string Targets__c;

        public override string ToString()
        {
            return string.Format("{0}ing Change Request Account {1} to Territory {2}", Action__c, AccountUniqueIntegrationId__c, TerritoryUniqueIntegrationId__c);
        }
    };
}
