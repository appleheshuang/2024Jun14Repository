using MainFramework;
using MainFramework.GlobalUtils;
using OMREngineTest.Constructors;
using OMREngineTest.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OMREngineTest.ScenarioUtils.Jobs
{
    public class AdjudicationJob : OptimizerJob
    {
        public AdjudicationJob(TenantConfig tenant_info, eToken token, string id)
            : base(tenant_info, token, id)
        {

        }

        public HttpStatusCode SubmitAdjudicationSimulationJob(AdjudicationApiRequestBody request)
        {
            HttpStatusCode _statuscode = SubmitJob("adjudication", request);
            api_info = api._request;
            if (api._response != null) api_info += api._response;
            if (_statuscode == HttpStatusCode.Unauthorized) api_info += api._auth;
            return _statuscode;
        }

        public HttpStatusCode SubmitAdjudicationUserAssignmentJob(AdjudicationApiRequestBody request)
        {
            HttpStatusCode _statuscode = SubmitJob("adjudicationUserAssignment", request);
            api_info = api._request;
            if (api._response != null) api_info += api._response;
            if (_statuscode == HttpStatusCode.Unauthorized) api_info += api._auth;
            return _statuscode;
        }
    }
}
