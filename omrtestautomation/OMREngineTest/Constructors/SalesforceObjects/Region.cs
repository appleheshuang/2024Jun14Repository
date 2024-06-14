namespace OMREngineTest
{
    public class Region
    {
        public string Name;
        public string Description__c;
        public string EffectiveDate__c;
        public string EndDate__c;
        public string SOMRegionId__c;
        public string Status__c;
        public string UniqueIntegrationId__c;

        public override string ToString()
        {
            return string.Format("Name: {0}", Name);
        }
    };
}
