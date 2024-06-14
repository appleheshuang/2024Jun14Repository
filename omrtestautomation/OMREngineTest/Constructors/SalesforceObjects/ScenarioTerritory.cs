using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest
{
    public class ScenarioTerritory
    {
        [JsonIgnore]
        public string Id;
        public string Action__c;
        public string OMScenarioId__c;
        public string SOMTerritoryId__c;
        public string Type__c;
        public string Name;
        public string ShortName__c;
        public string SubType__c;
        public string SOMRegionId__c;
        public string RegionUniqueIntegrationId__c;
        public string ExternalId1__c;
        public string ExternalId2__c;
        public string ExternalId3__c;
        public string ExternalId4__c;
        public string ExternalId5__c;
        public string UniqueIntegrationId__c;
        //Use this if we want to ignore Cascade instead of default to false
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //public bool? Cascade__c;
        public bool Cascade__c;
        public string SOMSalesForceId__c;
        public string SalesForceUniqueIntegrationId__c;
        public string TerritorySalesForceType__c;
        public string AddressLine1__c;
        public string AddressLine2__c;
        public string AddressLine3__c;
        public string City__c;
        public string State__c;
        public string ZipCode__c;
        public string Extension__c;
        public string Country__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LookupDate__c;
        public string RefSalesForceName__c;
        public string RefRegionName__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;

        public override string ToString()
        {
            return string.Format("{0}ing Territory Name {1}", Action__c, Name);
        }
    };
}
