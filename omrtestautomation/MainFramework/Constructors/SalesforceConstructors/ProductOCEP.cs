namespace MainFramework
{
    public class ProductOCEP
    {
        public string Id;
        public string Name;
        public string OCE__UniqueIntegrationId__c;

        public bool ShouldSerializeId()
        {
            return false;
        }
    };


}
