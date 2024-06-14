using MainFramework;

﻿namespace MainFramework
{
    public class TenantConfig
    {
        public string orgURL { get; set; }
        public string baseURL { get { return orgURL; } set { orgURL = value; } }
        public string clientID { get; set; }
        public string clientSecret { get; set; }
        public string orgUserName { get; set; }
        public string userName { get { return orgUserName; } set { orgUserName = value; } }
        public string orgPassword { get; set; }
        public string password { get { return orgPassword; } set { orgPassword = value; } }
        public string orgid { get; set; }
        public string sfApiKey { get; set; }
        public string generic_sfApiKey { get; set; }
        public string lexiApiKey { get; set; }
        public string TenantId { get; set; }
        public string TenantUser { get; set; }
        public string TenantPassword { get; set; }
        public string snowConnection { get; set; }
        public string sfnamespace { get; set; }
        public string Parameters { get; set; }
        public string targetEnv { get; set; }
        public string targetVersion { get; set; } // not required
        
        // Users required for adjudication, defaults=rep@<domain>.com / <orgPassword>
        public string adjRepUser { get; set; }
        public string adjDmUser { get; set; }
        public string adjHoUser { get; set; }
        public string adjRepPassword { get; set; }
        public string adjDmPassword { get; set; }
        public string adjHoPassword { get; set; }

        public string OCEDVersion { get; set; }
        public string OCESegVersion { get; set; }
        private string _endpoint { get; set; }
        public string endpoint
        {
            get
            {
                EnvFixture _api = new EnvFixture("envConfig-" + targetEnv + ".json");
                return _endpoint ??= _api.Config.Url;
            }
            set
            {
                _endpoint = value;
            }
        }
        public string s3bucket
        {
            get
            {
                EnvFixture _api = new EnvFixture("envConfig-" + targetEnv + ".json");
                return _api.Config.TestDataBucket;
            }
        }
        public bool snowchecks_enabled
        {
            get { return tenant_exists && !(snowConnection is null || snowConnection == ""); }
        }
        public bool tenant_exists
        {
            get { return !(TenantId == "" || TenantId is null); } 
        }
        public string mode { get; set; }
        public bool OceMode
        {
            get { return (mode ?? "OMR").StartsWith("OCE"); }
        }
    }
}
