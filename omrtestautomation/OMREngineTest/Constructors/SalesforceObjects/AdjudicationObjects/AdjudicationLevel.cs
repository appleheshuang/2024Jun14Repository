using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects
{
    public class AdjudicationLevel
    {
        [JsonIgnore]
        public string Id; //Required
        public string TerritoryType__c; //Required
        public string ResponseDays__c;
        public string PermittedActions__c;
        public string AutomaticAction__c;
        public string OMAdjudicationId__c; //Required
        public string Role__c; // REQ,APPR,HO
    }
}
