using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects
{
    class AdjudicationGuardRail
    {
        [JsonIgnore]
        public string Id; //Required
        public string OMAdjudicationId__c; //Required
        public string TerritoryType__c; //Required
        public string MaxGeographyChange__c;
        public string MaxGeographyPercent__c;
        public string MaxAccountChange__c;
        public string MaxAccountPercent__c;
        public string MaxSegmentChange__c;
        public string MaxSegmentPercent__c;
        public string MaxFrequencyChange__c;
        public string MaxFrequencyPercent__c;
    }
}
