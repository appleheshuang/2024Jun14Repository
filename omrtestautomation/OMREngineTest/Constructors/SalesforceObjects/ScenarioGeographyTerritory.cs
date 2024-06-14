using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioGeographyTerritory
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMGeographyId__c;
        public string GeographyUniqueIntegrationId__c;
        public string SOMTerritoryId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string Purpose__c;
        public string ExternalId1__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string SOMGeographyTerritoryId__c;
        public string Source__c;
        public string RefGeographyName__c;
        public string RefGeographyType__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string RefSalesForceName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing Geography {1} to Territory {2}", Action__c, GeographyUniqueIntegrationId__c, TerritoryUniqueIntegrationId__c);
        }
    };
}
