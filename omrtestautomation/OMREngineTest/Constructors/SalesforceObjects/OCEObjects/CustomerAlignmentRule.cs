using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.OCEObjects
{
    class CustomerAlignmentRule
    {
        [JsonIgnore]
        public string Id;
        [JsonIgnore]
        public string Name;
        public string Account__c;
        public string AddedTerritories__c;
        public string ApprovalStatus__c;
        public string DroppedTerritories__c;
        public string OfflineCreatedById__c;
        public string OfflineCreatedDate__c;
        public string OfflineLastModifiedById__c;
        public string OfflineLastModifiedDate__c;
        public string OfflineUniqueId__c;
        public string Territories__c;
        public string UniqueIntegrationID__c;
    }
}
