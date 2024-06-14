using Amazon.DynamoDBv2.DataModel;
using System;

namespace MainFramework
{
    [DynamoDBTable("OMRTenantConfigurations")]
    public class OMRTenantConfigurations
    {
        public OMRTenantConfigurations() { }

        [DynamoDBHashKey]
        public string IdentityId { get; set; }

        [DynamoDBProperty]
        public string TenantId { get; set; }

        [DynamoDBProperty]
        public string TenantName { get; set; }

        [DynamoDBProperty]
        public string LastSync { get; set; }

        [DynamoDBProperty]
        public int NoOfTerritories { get; set; }

        [DynamoDBProperty]
        public string TenantType { get; set; }

        [DynamoDBProperty]
        public string ComputeType { get; set; }

        [DynamoDBProperty]
        public SnowflakeCredentials Snowflake { get; set; }

        [DynamoDBProperty]
        public string ParentTenantId { get; set; }

        [DynamoDBProperty]
        public DateTime CreatedOn { get; set; }

        [DynamoDBProperty]
        public string Status { get; set; }

        [DynamoDBProperty]
        public DateTime StatusChangedOn { get; set; }

        [DynamoDBProperty]
        public ApiKeysNode ApiKeys { get; set; }

        [DynamoDBProperty]
        public OMRNode OMR { get; set; }

    }

    public class SnowflakeCredentials
    {
        public string ConnectionString { get; set; }
        public SnowflakeWarehouseNames Warehouses { get; set; }
        public string SnowflakeRoleName { get; set; }
    }

    public class SnowflakeWarehouseNames
    {
        public string APIWarehouseName { get; set; }
        public string ComputeWarehouseName { get; set; }
    }

    public class ApiKeysNode
    {
        public string LexiApiKey { get; set; }
        public string SalesForceApiKey { get; set; }
    }

    public class OMRNode
    {
        public string Namespace { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string Password { get; set; }
        public string SalesForceInstanceURL { get; set; }
        public string User { get; set; }
    }


}
