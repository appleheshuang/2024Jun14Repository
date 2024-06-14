using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects
{
    public class Adjudication
    {
        [JsonIgnore]
        public string Id; //Required
        public string Name; //Required
        public string Type__c; //Required
        public string ReviewStartDate__c; //Required
        public string ReviewEndDate__c; //Required
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AdjudicationEffectivedate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AdjudicationEndDate__c;
        public string OMRegionId__c; //Required
        public string OMScenarioId__c;
        public string AdjudicationStatus__c; //Required
        public string RefScenarioUniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefScenarioEffectiveDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefScenarioEndDate__c;
        public string RefScenarioProcessingEndTime__c;
        public bool AutoApplyOnManagerApproval__c;
        public string UniqueIntegrationId__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ReviewStartDate1__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ReviewEndDate1__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ReviewStartDate2__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ReviewEndDate2__c;
        public string UserAssignmentType__c; //Required
        public string SOMEngagementPlanId__c;
        public string RefEngagementPlanName__c;
        public string AdjudicationScope__c;


        public Adjudication setFieldsToUniqueIntegrationID(string adjudicationName, string adjudicationUID)
        {
            this.Name = adjudicationName + "_" + adjudicationUID;
            this.UniqueIntegrationId__c = adjudicationUID;
            return this;
        }
        public string setType()
        {
            if ((this.Type__c == null || this.Type__c == "") && this.AdjudicationEffectivedate__c != null && this.AdjudicationEffectivedate__c != "")
                this.Type__c = DateTime.Parse(AdjudicationEffectivedate__c) <= DateTime.Now ? "CURRENT" : "FUTURE";
            return this.Type__c;
        } 
        public bool IsCurrent() { return setType() == "CURRENT"; } 
    }
}
