namespace MainFramework
{
    public class ProductHierarchy
    {
        public string SOMProductHierarchyId__c;
        public string Name;
        public string UniqueIntegrationId__c;
        public string OMParentProductId__c;
        public string OMChildProductId__c;
        public string RelationType__c;
        public string Cascade__c;
        public string CascadeStatus__c;
        public string EffectiveDate__c;
        public string EndDate__c;
        public string Status__c;
        public string PriorEffectiveDate__c;
        public string PriorEndDate__c;
        public string PriorStatus__c;
        public bool IsPending__c;
        public string ExternalId1__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    };


}
