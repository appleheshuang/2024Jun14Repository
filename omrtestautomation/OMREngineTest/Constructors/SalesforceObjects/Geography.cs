using Newtonsoft.Json;

namespace OMREngineTest
{
    public class Geography
    {
        public string Id;
        public string Code__c;
        public string Type__c;
        public string EffectiveDate__c;
        public string EndDate__c;
        public string SOMGeographyId__c;
        public string Status__c;
        public string UniqueIntegrationId__c;
        public string Country__c;
        public string OMRegionId__c;

        public override string ToString()
        {
            return string.Format("SOMGeographyId: {0}", SOMGeographyId__c);
        }
    };
}
