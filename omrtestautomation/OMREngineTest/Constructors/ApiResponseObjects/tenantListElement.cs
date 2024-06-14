using System;

namespace OMREngineTest.Utils
{
    public struct tenantListElement
    {
        public string identityId;
        public DateTime createdOn;
        public TenantComputeType computeType;
        public TenantType tenantType;
        public int noOfTerritories;
        public apikeys_node apiKeys;
        public string statusChangedOn;
        public TenantStatus status;
        public version_node version;
        public string tenantId;
        public string tenantName;
    }

    public enum TenantType {PROD,SBOX};
    public enum TenantStatus {ACTV,INAC};
    public enum TenantComputeType {Shared,Dedicated};

    public struct apikeys_node
    {
        public string lexiApiKey { get; }
        public string salesForceApiKey { get; }
    }

    public struct version_node
    {
        public string currentVersion { get; }
        public string previousVersion { get; }
        public string status { get; }
    }
}
