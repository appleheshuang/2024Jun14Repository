using System;
using System.Net;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework;
using MainFramework.GlobalUtils;
using MainFramework.TestUtils;
using System.Collections;
using OMREngineTest;

namespace ScenarioTests
{

    [TestCaseOrderer("MainFramework.TestUtils.PriorityOrderer", "MainFramework")]
    [Trait("TestRailProjectId", "95")] // OMR
    [Trait("TestRailSuiteId", "3064")] // Engine tests
    [Trait("TestRailSectionId", "488459")] // Scenario API security
    [Trait("TestType", "Auto-Core")]
    [Trait("custom_testtype", "5")] //
    [Trait("custom_tstype", "1")]
    [Trait("custom_testexecution", "2")]
    public class ScenarioAPISecurity
    {
        private RequestSender _sender;

        public s3TenantFixture tenant;
        public string tenantconfigfile = "tenant-nosforg.json";

        private string baseURL; private string tokenEndpoint; private string calcEndpoint;
        private string xapikey; private string tenantUser; private string tenantPassword;
        //private string token; private string refreshtoken;

        private string ScenarioId = "fake";
        private bool tenantconfig_available;

        public ScenarioAPISecurity(ITestOutputHelper output)
        {
            tenantconfig_available = true;
            //tenant info
            try {
                tenant = new s3TenantFixture(tenantconfigfile);
                tenantUser = tenant.Config.TenantUser;
                tenantPassword = tenant.Config.TenantPassword;

                //Global API signature & codes - These should be public globally
                EnvFixture _api = new EnvFixture("envConfig-" + tenant.Config.targetEnv + ".json");
                globalCodes _endpoint = new globalCodes("EndPoints.json");
                baseURL = _api.Config.Url + _api.Config.EnvRoot;
                calcEndpoint = baseURL + _endpoint["scenarioCalc"];
                tokenEndpoint = baseURL + _endpoint["token"];
                xapikey = tenant.Config.sfApiKey ?? _api.Config.master_apikey;

                _sender = new RequestSender();
            }
            catch
            {
                tenantconfig_available = false;
            }
        }

        [Theory, TestPriority(0)]
        [Trait("References", "OMR-243")]
        [InlineData(HttpStatusCode.Unauthorized, false, null, null)]
        [InlineData(HttpStatusCode.Unauthorized, false, "Invalid", "Credentials")]
        [InlineData(HttpStatusCode.OK)]
        public void TryGetToken(Enum expected_status, bool token_expected = true, string user = "", string pswd = "")
        {

            if (tenantconfig_available && (!token_expected || tenantPassword != "" && tenantPassword != null))
            {
                Hashtable headers = new Hashtable();
                headers.Add("Content-Type", "application/json");
                headers.Add("x-api-key", xapikey);

                string Creds = token_expected ? tenantUser + ":" + tenantPassword : user + ":" + pswd;
                headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Creds)));

                Tuple<HttpWebResponse, RequestSender.TokenApiResponseBody> resptoken = _sender.FetchResponse<RequestSender.TokenApiResponseBody>(tokenEndpoint, headers, "NONE", "POST");

                Assert.Equal(expected_status, resptoken.Item1.StatusCode);
                if (token_expected)
                {
                    Assert.NotNull(resptoken.Item2.token);
                    Assert.NotNull(resptoken.Item2.refresh);
                }
                else
                    Assert.Null(resptoken.Item2);
            }
        }

        [Fact, TestPriority(40)]
        [Trait("References", "OMR-243")] //OMR-243 - Session based authentication (POC) 
        public void SubmitScenarioWithExpiredToken()
        {
            //get tokens
            var token = "eyJraWQiOiI3dVIyVmJGV0d3dTFhb1c4VWRCR0JZZ0NWbTUwNGNQRnFTeUdJc3JvbTBvPSIsImFsZyI6IlJTMjU2In0.eyJjb2duaXRvOnJvbGVzIjpbImFybjphd3M6aWFtOjo2MTgyMjA3MzYwNTg6cm9sZVwvc2VydmljZS1yb2xlXC9vcHRpbWl6ZXJhcHNpcHJvZGVudl9PTVJFbmdpbmVfRGVmYXVsdFJvbGVfYXAtc291dGhlYXN0LTEiXSwic3ViIjoiYjljYmZjODQtOWU1OC00N2M3LTg2MmQtNjcwMjEwNjEwNTY1IiwiYXVkIjoiN28wM2loczRqcDU4OWg2MnJtNmZ0bDRvdGIiLCJjb2duaXRvOmdyb3VwcyI6WyJvcHRpbWl6ZXJhcHNpcHJvZGVudl9PTVJfVGVuYW50X0NvZ25pdG9fVXNlcnNfZ3JvdXAiXSwiZXZlbnRfaWQiOiIxNjBlYzI3OS1hNzNiLTQzNGMtYTIyNC0yZGZiYmQxMjlmYTIiLCJ0b2tlbl91c2UiOiJpZCIsImF1dGhfdGltZSI6MTYwODI0NjQ3NiwiaXNzIjoiaHR0cHM6XC9cL2NvZ25pdG8taWRwLmFwLXNvdXRoZWFzdC0xLmFtYXpvbmF3cy5jb21cL2FwLXNvdXRoZWFzdC0xXzBPVWhGZmxNWCIsImNvZ25pdG86dXNlcm5hbWUiOiIxYThlYzdmMmI2OWY0MzUwOWMzZGE4MjlkNjVmMmE4OSIsImV4cCI6MTYwODI1MDA3NiwiaWF0IjoxNjA4MjQ2NDc2fQ.b-upqK-8_lWmcLEfvm8aSEuL__8_vdmgY6cXaQQpa6mkuKfdWNvueLgfRmjU_EQKEarUmr5XNLNRafrHnfjRmXSfgizT9TueHxX6JLMkFDbPdLchf9g6zThr1lvSwPdwazTqEMGCgzhFC43p1hGmSPKGI0Kb1Bh7XsF_twlaR3k1iQac6b-aH_MefgS8tSuEU4yWrxiIQqduWoIfwntkAuiR2N2NrPo8dpqDYiyhwQIWoECGJLuHPojFsoGRKWEX1jIjhC_dmx-Mz8ycWFXYRxlcugLfZc7_OQe1IHvLhjBkpyWdeXjiu2KvdUdwuyak84y8rAGhx-pb1Sm7nd3Sww";
            var refreshtoken = "eyJjdHkiOiJKV1QiLCJlbmMiOiJBMjU2R0NNIiwiYWxnIjoiUlNBLU9BRVAifQ.jpEo48XAAqgbTPbIup8qP20E9txuqqVDoc02e0rCAXti2hGSyChCXfwc_g09jwbtZ8lQ66wW3lrRW3jbil1_L0BvejqEwe8EBzgbDxZpw0yLWP3_XkjyY_664lulqj02oJWMBgaUwzMlIUw_tjlfBOhnZl8eCDVgi_Sitpwymp5NyvGyhnk4rMl07L2HiCoZUYxiGdahtGABi6Ls3oWm66lQlJ4thRwLA1UQWF18-Aw0P9pApDFdR5CDUL35G6ygHjOT02e6wgyG4SpxKGc1u9a5mAOILiQecSniYAiAR7b-Wjs1gjAEkJ0JsOwYfyYTX2C0nl3T7sh1RpnJe145oA.Jkhr73JmwLB-hNGs.122H-0D-xKmP2vGOPCTi0Weu_X6F_rgIHy9Uhx7ndnURJBiVqBrgXUAaKkj7b6iLYxtcvURzQaZYvuTkolqy3ltDRsJp3mKufRxZiTYtvs1nAEU5x9o34m6CiT3ravcEOW4inW2eVzxXthN2Q7lwvWV4mCUJGvGcv4yytunj4YBCBnMhhy6TWzubHFvs93czXuhRjS-sFmt62h9Tn_LtkNkWNZ947NDMQFLKPjvAE-X4z5OPUUhAeStTPv3i41WW0QCAXUBdUOncSQnm_lyDK4LqOMIaaMrz-OE4oTeqcCIoNT9m0s1zS_NWvB-bvzKqltO2Vkvi1wFNkuWlamSi1wlgopwzkxljKwo_NF28C6JbDbunpuVCBFDVmxtI8fC9I9kwgCzH3Oq3auMX92uPNaypuLU59oihFi-uzsuSubrnCuHH_aMtuRL_kBtoX-r8assd-ucdX118L1ZfdtS_eQn4b_u7H3niV0goNQSkCYIkMoT2362AMO7dYugAPUHKyHzM6G_V9SphH5y8NNr01c0Y5aegLCipnf55xbuNFbvBCFS2pTDIcX_KL-cwcLte4cCPks-mhFar2X22T6xnht3xYzzmUe2SM-oltst31CTeRr7PkYf-hobti_u8JU7TtYVA7Ja-sbj4F0EETf2L7_5ltXA6l8nzqAf81ttGp6AHMYFPdaYVj-k8QGlX2UFxbie91G70GbBlsZMStguyYvBPbzoI8bPNLheWUfIgEqjnvbgMRk-VBtgMaYimSxDzLor_UTFx3dB3fO0sFHi_dTBysd0e9yEP4M66YbwM3OMg8wq19iZjtjmBC2WhkLVG9sjnsJr8gEAzRAWF1Mf7LkgX-KjEMJ6Z0-hlFZoMh-YrT5Tfcb2fXEO6L59OONjjWwZzwSYWOHH1VcRQDM4Oj8yZQwSmYMKKW1_fUxfAtF3XUIaZXsBtm50M8bnPIi-BImb5vAb-qDPZyfQTO5iR_GWcAOszR9bPvQcPM-3gW2N6rkbgLu8k4hL2nG_CBciFvAYrR-rgIer2ZBv7ItVjRsolsxSZmLvOZLcNfXOlC7hghgTygxTa-uuK4gHF9tNiPcl2Z34Waabfib5CJpXqRBoqiYjt0iif-8Zb6QGXbDIpWsHc3goWifsvQo652KxJ1iMG--R4WaTOuMNy1g9ijmrnWybf3cHBGUZOr49w7gqoGxgClP6ez0EDK6vzgeJqHEGBpNqLhDWOX_m78nKIPytIjNib3Wcls8jPeZtlOJ0SBhpHym0DnSnQ8wQTtQW8U7PKjNpBvHePyXboD-Mp8RhbwnJmpqEJU0u8mnOJ1u9KR3Pr9E8cplut41Dpdm24ZDQmJkns.0QudH6pZbJBy-Iu7k0FWTA";

            //update request headers
            Hashtable headers = new Hashtable();
            headers.Add("Content-Type", "application/json");
            headers.Add("x-api-key", xapikey);
            headers.Add("Authorization", "Bearer " + token);
            headers.Add("refreshtoken", refreshtoken);

            //set scenario id
            var scenarioRequestBody = new ScenarioApiRequestBody(ScenarioId);
            var body = JsonConvert.SerializeObject(scenarioRequestBody);

            //run scenario
            Tuple<HttpWebResponse, RequestSender.TokenApiResponseBody> scenario = _sender.FetchResponse<RequestSender.TokenApiResponseBody>(calcEndpoint, headers, body, "POST");

            //assert request status
            var response = scenario.Item1;
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        }

    }
}
