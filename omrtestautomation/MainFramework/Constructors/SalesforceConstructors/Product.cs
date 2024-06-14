namespace MainFramework
{
    public class Product
    {
        public string Id;
        public string name;
        public string Description__c;
        public string EffectiveDate__c;
        public string EndDate__c;
        public string UniqueIntegrationId__c;
        public string SOMProductId__c;
        public string Status__c;
        public string Type__c;
        public bool CompetitorProduct__c;
        public string OMRegionId__c;
        public string CascadeStatus__c;
        public bool ShouldSerializeId()
        {
            return false;
        }
    };


}
