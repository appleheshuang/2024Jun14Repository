namespace MainFramework
{
    public class OCEAddress
    {
        public string Id;
        public string Name;
        public string OCE__Brick__c;
        public string OCE__CountryCode__c;
        public string OCE__UniqueIntegrationID__c;
        public bool ShouldSerializeId()
        {
            return false;
        }
    }
}
