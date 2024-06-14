using Newtonsoft.Json;
using OMREngineTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Optimizer.Constructors
{
    public class DataSyncApiRequestBody : ApiRequestBody
    {
        public string Target;
        public string Tables;
        public string JobType;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Retry;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Order;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<DataSyncMapping> Mapping;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int Batch;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string API;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LastUpdate;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StartDate;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<DataSyncFilters> Filters;

        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //public string Tracked;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string PreProcessing;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Processing;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string PostProcessing;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string JobId;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SalesforceEnvironmentType;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Mode;

        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //public DataSyncParameters Parameters;

        public class DataSyncFilters
        {
            public string Table;
            public string Filter;
        }

        public class DataSyncMapping
        {
            public string Table;
            public bool MapByName;
            public List<DataSyncColumns> Columns;
        }

        public class DataSyncColumns
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Snowflake;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Salesforce;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? Exclude;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? MapByName;
        }

        //public class DataSyncParameters
        //{
        //    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //    public string InactivateAccountsDeletedInOCESales;

        //    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //    public string ExplicitAccountTerritoryAlignment;

        //    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //    public string InactivateUncalledOnUserCreatedAlignments;
        //}

        //public interface IDataSyncJobParameters { }
        //public class StaticSync : IDataSyncJobParameters
        //{

        //}
        //public class ACCOUNT_PULL : IDataSyncJobParameters
        //{
        //    public bool InactivateAccountsDeletedInOCESales;
        //}

        //public class TestPull : IDataSyncJobParameters
        //{

        //}

        //public class TestPush : IDataSyncJobParameters
        //{

        //}

    }
}

