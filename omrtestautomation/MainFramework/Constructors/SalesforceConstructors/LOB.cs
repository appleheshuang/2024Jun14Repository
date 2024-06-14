
namespace MainFramework
{
    public class LOB
    {
        public string Id;
        public string Name;
        public string Description__c;
        public string SOMLineOfBusinessId__c;
        public string Status__c;
        public string UniqueIntegrationId__c;
        public string ExternalIdType__c;
        public string BusinessUnitId__c;
        

        public bool ShouldSerializeId()
        {
            return false;
        }
    }
}
