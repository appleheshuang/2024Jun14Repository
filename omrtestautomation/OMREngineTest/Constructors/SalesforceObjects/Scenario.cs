using Newtonsoft.Json;

namespace OMREngineTest
{
    public class Scenario
    {
        [JsonIgnore]
        public string Id;
        [JsonIgnore]
        public string SOMRegionId__c; //Required to use in commit by region story
        public string Name;
        public string Description__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate__c;
        public string Type__c;
        public string OMRegionId__c;
        public string ScenarioStatus__c;
        public string ErrorMessage__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string OMAdjudicationId__c;

        public Scenario SetFieldsToUniqueIntegrationId(string ScenarioName, string testUniqueIntegrationId)
        {
            this.Description__c = unique(this.Description__c, ScenarioName,testUniqueIntegrationId,256);
            this.Name = unique(this.Name, ScenarioName, testUniqueIntegrationId);
            this.UniqueIntegrationId__c = unique(this.UniqueIntegrationId__c, ScenarioName, testUniqueIntegrationId,30);
            return this;
        }
        private string unique(string orig, string defaultname, string uid, int max_length=80)
        {
            string given = orig is null || orig == "" ? defaultname : orig;
            string prefix = given is null || given == "" ? "" : given + "_";
            if (given.Contains(uid))
                return given.Length <= max_length ? given : given.Substring(0, max_length);
            else
                return (prefix + uid).Length <= max_length ? prefix + uid : prefix.Substring(0, max_length - uid.Length) + uid;
        }

        public override string ToString()
        {
            return string.Format("Scenario {0} Name {1}", UniqueIntegrationId__c, Name);
        }
    };
}
