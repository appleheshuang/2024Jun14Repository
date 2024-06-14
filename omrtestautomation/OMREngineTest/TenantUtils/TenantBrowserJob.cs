using System;
using System.Net;
using RestSharp;
using MainFramework;
using MainFramework.GlobalUtils;

namespace OMREngineTest.Utils
{

    public class TenantBrowserJob : OptimizerApi
    {

        public TenantBrowserJob(TenantConfig t)
            : base(t, new eToken())
        {
        }

        public string CreateBrowserUser()
        {
            //Get a token for the tenant
            Tuple<IRestResponse, String> response = Request_RestSharp("browser", "POST");
            return HttpStatusCode.OK == response.Item1.StatusCode ? response.Item2.ToString() : response.Item1.ToString() + response.Item2.ToString();
        }

    }
}