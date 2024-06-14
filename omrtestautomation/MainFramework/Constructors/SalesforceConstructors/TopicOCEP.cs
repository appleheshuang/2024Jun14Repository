
using Newtonsoft.Json;

namespace MainFramework
{
    public class TopicOCEP
    {
        public string Id;
        public string Name;
        public string UniqueIntegrationId__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    }


}
