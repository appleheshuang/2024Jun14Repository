
namespace MainFramework
{
    public class JourneyActivity
    {
        public string Id;
        public string SOMJourneyActivityId__c;
        public string SOMJourneyId__c;
        public string VersionId__c;
        public string ActivityId__c;
        public string ActivityName__c;
        public string ActivityExternalKey__c;
        public string ChannelCode__c;
        public string JourneyActivityKey__c;
        public string JourneyActivityObjectId__c;
        public string JourneyActivityTypeCode__c;
        public string JourneyId__c;
        public string VersionNumber__c;
        public string SOMCreatedById__c;
        public string SOMLastModifiedById__c;
        public string SOMCreatedDate__c;
        public string SOMLastModifiedDate__c;
        public bool ShouldSerializeId()
        {
            return false;
        }
    }
}
