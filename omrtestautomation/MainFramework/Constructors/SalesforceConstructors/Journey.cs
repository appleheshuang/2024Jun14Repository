
namespace MainFramework
{
    public class Journey
    {
        public string Id;
        public string SOMJourneyId__c;
        public string VersionId__c;
        public string Name__c;
        public string Description__c;
        public string JourneyId__c;
        public string SOMLineOfBusinessId__c;
        public string VersionNumber__c;
        public string JourneyStatus__c;
        public string LastPublishedDate__c;
        public string EventDefinitionId__c;
        public string EventDefinitionKey__c;
        public string DataExtensionName__c;
        public string JourneyEntryMode__c;
        public string SOMCreatedById__c;
        public string SOMLastModifiedById__c;
        public string SOMCreatedDate__c;
        public string SOMLastModifiedDate__c;
        public string ExternalId1__c;
        public string SourceId__c;
        public string ApprovalDate__c;
        public string ApprovalInformation__c;
        public string ApprovedBy__c;
        public string CRMShare__c;
        public string RefJourneyTopic__c;
        public string JourneyType__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    }
}
