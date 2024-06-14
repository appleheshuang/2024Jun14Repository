using System;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;
using MainFramework;
using MainFramework.GlobalUtils;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using OMREngineTest.ScenarioUtils.DataUnmarshallers.UnmarshallerUtils;

namespace OMREngineTest.ScenarioUtils.Jobs
{
    /*
     * An OCESyncJobRequestBody object can take any /ocesync JSON and dynamically resolve the Parameters field to a subclass of IOCESyncJobParameters.
     * IOCESyncJobParameters can be PUBLISH_OTHER_OMR_DATA, PUBLISH_CUSTOMER_ALIGNMENT, PULL_ACCOUNT or INITIAL_LOAD.
     * 
     * Unmarshalling into an object this way allows for parameterisation of fields (StartFrom or otherwise)
     */
    //[JsonConverter(typeof(OCESyncJobConverter))]
    public class OCESyncJobRequestBody : ApiRequestBody
    {
        public const string Type = "OCESync";
        public string Subtype;
        public string Mode;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StartFrom;

        public OCESyncJobRequestBody(string job_type, string tenant_mode, string job_params, string tenant_params, string start_from = null)
            : base(job_params, tenant_params)
        {
            Subtype = job_type;
            Mode = tenant_mode;
            StartFrom = start_from ?? "";
        }
    }
/*
    public interface IOCESyncJobParameters { }
    public class PUBLISH_OTHER_OMR_DATA : IOCESyncJobParameters
    {
        public string EtmTerritoryModelName;
        public string EtmTerritoryTypeName;
        public string RuleEngineEnabled;
        public string PopulateUserManager;
        public string OCEPersonalGeographyTypeMapping;
    }
    public class PUBLISH_CUSTOMER_ALIGNMENT : IOCESyncJobParameters
    {
        public string RuleEngineEnabled;
    }

    public class PULL_ACCOUNT : IOCESyncJobParameters 
    {
        public string ExplicitAccountTerritoryAlignment;
        public string InactivateAccountsDeletedinOCESales;
        public string OCEAdditionalTables;
    }

    public class INITIAL_LOAD : IOCESyncJobParameters
    {
        public string OCEObjects;
        public string EtmTerritoryModelName;
        public string EtmTerritoryTypeName;
        public string DefaultRegion;
        public string EffectiveDate;
        public string EndDate;
    }
    */
    public class OCESyncJob : OptimizerJob
    {
        public OCESyncJob(TenantConfig t, eToken token, string jobid=null)
            : base(t, token, jobid) { }

        public HttpStatusCode SubmitOCESyncJob(OCESyncJobRequestBody parameters)
        {
            return SubmitJob("ocesync", parameters);
        }
        new public bool IsSuccessful(int waitime = 300)
        {
            string success_or_error = _status_code["job_success"] + ',' + _status_code["job_error"];
            string act_status = WaitForStatus(success_or_error, waitime);
            if (act_status == _status_code["job_success"])
                return true;
            else if (act_status == _status_code["job_error"])
            {
                Tuple<bool,string> errors = HaveOtherThan255charLimitErrors();
                if (errors.Item1)
                {
                    Console.WriteLine("Sync Error: " + errors.Item2);
                    return false;
                }
                else
                {
                    Console.WriteLine("Sync Warning: " + errors.Item2);
                    return true;
                }
            }
            else 
                return false;
        }

        private string GetErrorsSql(int past_mins = 10, bool include_warnings = false)
        {
            string log_types = include_warnings ? "'ERROR','WARNING'" : "'ERROR'";
            return "select \"LogDate\" || ' | ' || \"Message\" as \"Error\""
                    + " from static.\"OMLog\""
                    + " where \"Application\" = 'engine' and \"LogType\" in (" + log_types + ")"
                    + " and \"LogDate\" > timeadd(min, -" + past_mins + ", current_timestamp)";
        }
        private const string latest_first = " order by \"LogDate\" desc";

        private Tuple<bool,string> HaveOtherThan255charLimitErrors(int past_mins = 10)
        {
            if (snowq != null) {
                string message_match = "(\"Message\" like '%Concatenated territory names associated with geography exceeds 255 character OCE-Personal Limit%'"
                    + " or \"Message\" like 'OCESyncPublishOMDataJob failed for one or more entities%')";
                string get_other_errors = GetErrorsSql(past_mins) + " and not " + message_match + latest_first + " limit 1";
                string other_error = snowq.ReturnSingleColumnValueFromSnowFlake(get_other_errors, "Error");
                if (other_error == "{not found}")
                {
                    List<string> errors = GetSyncErrors(1, past_mins);
                    string first_error = errors.Count > 0 ? errors[0] : "";
                    return new Tuple<bool, string>(false, first_error);
                }
                else
                    return new Tuple<bool, string>(true, other_error);
            }
            else
                return new Tuple<bool, string>(true, "");
        }
        public List<string> GetSyncErrors(int max_errors = 5,int max_past_mins = 10)
        {
            if (snowq != null)
            {
                string get_errors = GetErrorsSql(max_past_mins, include_warnings: true) + latest_first + " limit " + max_errors;
                return snowq.ReturnWholeStringColumnValueFromSnowFlake(get_errors, "Error");
            }
            else
                return new List<string> { "Check Cloudwatch errors for details" };

        }
        
    }
}