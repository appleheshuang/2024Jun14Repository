using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects
{
    class AdjudicationRegion
    {
        [JsonIgnore]
        public string Name;
        public string SOMRegionId__c; //Required
        public string RegionUniqueIntegrationId__c;
        public string RegionRefName__c;
        public string OMAdjudicationId__c; //Required
    }
}
