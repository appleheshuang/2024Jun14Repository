using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Optimizer.Constructors.SalesforceObjects.OCEObjects
{
    class AccountTerritoryProduct
    {
        public string Id;
        [JsonIgnore]
        public string Name;
        public string Account__c;
        public string Decile__c;
        public string IntegrationID__c;
        public string OfflineCreatedByID__c;
        public string OfflineCreatedDate__c;
        public string OfflineLastModifiedByID__c;
        public string OfflineLastModifiedDate__c;
        public string OfflineUniqueId__c;
        public string Product__c;
        public string Territory__c;
        public string UniqueIntegrationID__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    }
}

