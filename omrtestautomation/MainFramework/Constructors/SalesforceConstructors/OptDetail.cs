
using Newtonsoft.Json;

namespace MainFramework
{
    public class OptDetail
    {
        public string Id;
        public string UniqueIntegrationID__c;
        public string AccountAddress__c;
        public string ChannelValue__c;
        public string Channel__c;
        public string Topic__c;
        public string Account__c;
        public string Opt__c;
        public string Subscription__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    }


}
