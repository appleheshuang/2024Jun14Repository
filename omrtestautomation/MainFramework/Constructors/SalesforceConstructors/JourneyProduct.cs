
namespace MainFramework
{
    public class JourneyProduct
    {
        public string Id;
        public string SOMJourneyProductId__c;
        public string SOMJourneyId__c;
        public string SOMProductId__c;
        public string Status__c;
        public string ExternalId1__c;
        public string SourceId__c;
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
