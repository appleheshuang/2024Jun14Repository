using Newtonsoft.Json;
using System.Collections.Generic;

namespace OMREngineTest
{
    public class ScenarioApiRequestBody : ApiRequestBody
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ScenarioId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string OMAdjudicationId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AdjudicationStatus { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Mode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SOMRegionId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<CustomValidation> CustomValidation { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ModelId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ModelOperation { get; set; }

        public ScenarioApiRequestBody() { }
        public ScenarioApiRequestBody(string scenarioId, string settings = null, string tsettings = null, string tmode = "OMR", string somRegionId = null)
            : base(settings, tsettings, tmode)
        {
            ScenarioId = scenarioId;
            Mode = tmode;
            SOMRegionId = somRegionId;
            if (LexiMode(tmode))
            {
                Mode = null;
                SOMRegionId = null;
            }
        }

        public ScenarioApiRequestBody(string scenarioId, string settings, string adjudicationId, string adjudicationStatus, string tsettings = null, string tmode = "OMR", string somRegionId = null)
    : base(settings, tsettings, tmode)
        {
            ScenarioId = scenarioId;
            Mode = tmode;
            OMAdjudicationId = adjudicationId;
            AdjudicationStatus = adjudicationStatus;
            SOMRegionId = somRegionId;
            if (LexiMode(tmode))
            {
                Mode = null;
                SOMRegionId = null;
            }
        }

    }

    public class CustomValidation
    {
        public string DeveloperName;
        public string ErrorCode;
        public string ValidationType;
        public string Query;
    }

}
