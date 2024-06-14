using Newtonsoft.Json;
using OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects;
using System.Collections.Generic;

namespace OMREngineTest.Constructors
{
    public class AdjudicationApiRequestBody
    {
        public string OMAdjudicationId;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string OMScenarioId;
        public List<string> Territories;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> AccessKeyFields;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> AccessDateFields;
        public string EffectiveDate;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<GeographyChangeRequest> GeographyChangeRequests;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<AccountChangeRequest> AccountChangeRequests;
        public List<AdjudicationLevel> AdjudicationLevel;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SimulationFor;
        public bool SkipStatusUpdate;
        public bool PullChangeRequests;
        public string SOMEngagementPlanId;
    }

    public class GeographyChangeRequest
    {
        public string SOMGeographyId;
        public string Action;
        public string SOMTerritoryId;
    }

    public class AccountChangeRequest
    {
        public string OMAccountId;
        public string Action;
        public string SOMTerritoryId;
        public string SOMEngagementSegmentId;
        public string Targets;
    }

}