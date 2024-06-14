namespace MainFramework
{
    public class AccountAddress
    {
        public string Id;
        public string Name;
        public string OCE__AddressLine2__c;
        public string OCE__AddressLine3__c;
        public string OCE__AddressLine4__c;
        public string OCE__City__c;
        public string OCE__State__c;
        public string OCE__ZipCode__c;
        public string OCE__Country__c;
        public string OCE__Inactive__c;
        public string OCE__UniqueIntegrationID__c;
        public string OCE__GeoLocation__Latitude__s;
        public string OCE__GeoLocation__Longitude__s;
        public string OCE__CountryCode__c;
        public bool ShouldSerializeId()
        {
            return false;
        }
    }
    
}
