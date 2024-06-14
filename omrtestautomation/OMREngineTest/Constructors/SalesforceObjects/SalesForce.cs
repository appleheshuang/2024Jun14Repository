using Newtonsoft.Json;

namespace OMREngineTest
{
    public class SalesForce
    {
        public string Id;
        public string Type__c;
        public string EffectiveDate__c;
        public string EndDate__c;
        public string SOMSalesForceId__c;
        public string Status__c;
        public string UniqueIntegrationId__c;
        public string OMRegionId__c;

        public override string ToString()
        {
            return string.Format("SOMSalesForceId: {0}", SOMSalesForceId__c);
        }
    };
}
