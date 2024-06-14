using AngleSharp.Dom;
using Google.Protobuf.WellKnownTypes;
using MainFramework;
using MainFramework.Constructors;
using MainFramework.GlobalUtils;
using Newtonsoft.Json.Linq;
using OMREngineTest.ScenarioUtils.Jobs;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Digital.DigitalUtils.Jobs
{
    class DigitalJob : OptimizerJob
    {
        public DigitalJob(DigitalTenantConfig t, eToken token, string id = null)
    : base(t, token, id)
        {
           
        }

        public Tuple<IRestResponse, String> SubmitProcessConsentJob()
        {
            return api.Request_RestSharp("processConsent", "POST", isDigital : true);
        }

        public Tuple<IRestResponse, String> DigitalDataSyncJob()
        {
            return api.Request_RestSharp("processConsent", "POST", isDigital: true);
        }

        public Tuple<IRestResponse, String> SubmitScheduleDatasync()
        {
            JObject body = new JObject();
            body.Add("Type", "ScheduledDataSync");
            body.Add("TenantId", t.TenantId);
            body.Add("JobType", "15min");
            return api.Request_RestSharp("submitSchedularJob", "POST", isDigital: true, request_body: body);
        }
    }
}
