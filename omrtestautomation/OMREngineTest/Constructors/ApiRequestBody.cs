using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OMREngineTest
{
    public class ApiRequestBody
    {
        public ApiRequestBody() { }
        public ApiRequestBody(string job_params, string tenant_params = "", string tmode = "OMR")
        {
            Parameters = new ParamConfig(job_params, tenant_params).GetSettings();
            if (LexiMode(tmode))
            {
                Parameters = null;
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public JObject Parameters { get; set; }

        protected bool LexiMode(string mode) { return mode.ToLower().Contains("lexi"); }

    }

}
