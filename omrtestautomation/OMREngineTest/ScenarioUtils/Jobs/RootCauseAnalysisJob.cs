using MainFramework;
using MainFramework.GlobalUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMREngineTest.Constructors;
using OMREngineTest.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.ScenarioUtils.Jobs
{
    class RootCauseAnalysisJob : OptimizerJob
    {

        public RootCauseAnalysisJob(TenantConfig t, eToken token)
            : base(t, token, null) { }

        public RootCauseApiResponseBody SubmitRCA(object rcaBody)
        {
            string jsonRCAResponse = api.Request_RestSharp("rca", "POST", rcaBody).Item2;
            try
            {
                RootCauseApiResponseBody response = JsonConvert.DeserializeObject<RootCauseApiResponseBody>(jsonRCAResponse, new JsonSerializerSettings()
                {
                    DateParseHandling = DateParseHandling.None
                });
                FormatRCAEffectiveDates(response);
                return response;
            }
            catch (Exception e)
            {
                string error_message = "Failed to parse RCA response ...\n" + jsonRCAResponse;
                Console.WriteLine(error_message);
                throw new Exception(error_message, e); 
            }   
        }

        public void ReplaceBeforeEffectiveDatesIfNewDb(RootCauseApiResponseBody rcaJobResponse, DateTime dbCreatedDateTime, DateTime timeNow)
        {
            if ((timeNow - dbCreatedDateTime).TotalDays < 1)
            {
                foreach (RootCauseRule rules in rcaJobResponse.rules)
                {
                    foreach (RootCauseData data in rules.data)
                    {
                        data.beforeEffective = "\"{*N*}\"";
                    }
                }
            }
        }
        public void FormatRCAEffectiveDates(RootCauseApiResponseBody rcaResponse)
        {
            foreach (RootCauseRule rule in rcaResponse.rules)
            {
                rule.effectiveCalculationDate = DateTime.Now.ToString("yyyy-MM-dd");
                rule.endCalculationDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
        }
    }
}
