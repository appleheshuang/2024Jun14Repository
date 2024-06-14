using System.Net;
using MainFramework;
using MainFramework.GlobalUtils;
using System.Collections.Generic;

namespace OMREngineTest.ScenarioUtils.Jobs
{

    public class RecalcJob : ScenarioJob
    {

        public RecalcJob(TenantConfig tenant_info, eToken token, string app)
            : base(tenant_info, token, null, app) { }
       
        public HttpStatusCode SubmitRecalc(string jobParam=null)
        {
            requestBody = new ApiRequestBody(jobParam, t.Parameters);
            return SubmitJob("recalc");
        }

        /// <summary>
        /// RecalcJobId - Given the OMJob start time, get the matching recalc job id
        /// </summary>
        /// <param name="start_time"></param>
        /// <returns></returns>
        public string RecalcJobId(string start_time)
        {
            string recalc = "select \"Id\" from \"STATIC\".\"OMScenario\""
                + " where \"Id\" like 'RECALC_%'"
                + " and to_char(CONVERT_TIMEZONE('UTC',\"ProcessingStartTime\"),'MM/DD/YYYY HH24:MI:SS') = '" + start_time + "'"
                + " order by \"Id\" limit 1";
            return snowq.ReturnSingleColumnValueFromSnowFlake(recalc, "Id"); 
        }

        /// <summary>
        /// NextRecalcJobs - Return the ids for the next x recalc jobs, starting after the given start time
        /// Used by performance tests to gather multiple recalc jobs after submitting in batch
        /// </summary>
        /// <param name="earliest_start_time"></param>
        /// <param name="jobs"></param>
        /// <returns></returns>
        public List<string> NextRecalcJobs(string earliest_start_time, int jobs = 1)
        {
            string recalc = "select \"Id\" from \"STATIC\".\"OMScenario\" where \"Id\" like 'RECALC_%'"
                + " and to_char(CONVERT_TIMEZONE('UTC',\"ProcessingStartTime\"),'MM/DD/YYYY HH24:MI:SS') >= '" + earliest_start_time + "'"
                + " order by \"Id\" limit " + jobs;
            return snowq.ReturnWholeStringColumnValueFromSnowFlake(recalc, "Id");
        }

        public bool HasDequeued(int max_dur_sec = default_max_dur)
        {
            if (HasStarted(max_dur_sec)) {
                scenario_id = RecalcJobId(StartTime());
                return true;
            }
            return false;
        }
    }
}