
using Newtonsoft.Json;

namespace MainFramework
{
    public class Opt
    {
        public string Id;
        public string UniqueIntegrationID__c;
        public string Active__c;
        public string Source__c;
        public string Account__c;
        public string ParentTopic__c;
        public string OptDate__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    }


}
