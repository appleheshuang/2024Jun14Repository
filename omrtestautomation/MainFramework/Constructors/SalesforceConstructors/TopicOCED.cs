
using Newtonsoft.Json;

namespace MainFramework
{
    public class TopicOCED
    {
        public string Id;
        public string Name;
        public string Description__c;
        public string OMParentTopicId__c;
        public bool CRMShare__c;
        public string Status__c;
        public string ExternalId1__c;
        public string SOMTopicId__c;
        public string SourceId__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    }


}
