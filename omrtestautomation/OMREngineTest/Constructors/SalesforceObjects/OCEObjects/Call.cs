using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.OCEObjects
{
    public class Call
    {
        public string Id;
        public string CallDateTime__c;
        public string CallType__c;
        public string Territory__c;
        public string Account__c;
        public string UniqueIntegrationID__c;
        public bool ShouldSerializeId()
        {
            return false;
        }
    } 
}
