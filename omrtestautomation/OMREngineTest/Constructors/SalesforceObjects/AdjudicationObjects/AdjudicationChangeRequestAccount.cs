using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects
{
    public class AdjudicationChangeRequestAccount
    {
        [JsonIgnore]
        public string Id; //Required
        public string OMAdjudicationId__c; //Required
        public string SOMTerritoryId__c; //Required
        public string OMAccountId__c; //Required
        public string RefAccountName__c;
        public string RefAccountAccountType__c;
        public string RefAccountType__c;
        public string RefAccountSpecialty__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string RefSalesForceName__c;
        public string AccountUniqueIntegrationId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string Action__c; //Required
        public string SubmitStatus__c; //Required
        public string ApprovalStatus__c;
        public string ReviewLevel__c;
        public string OMScenarioId__c;
        public string InitialLevel__c; //Required
        public string SimulationStatusRep__c;
        public string SimulationStatusApprover__c;
        public string Targets__c;
        public string SOMEngagementSegmentId__c;
        public string RefEngagementSegment__c;
        public string RefAddress__c;
        public string RefAddressText__c;
        public string FrequencyChangeCount__c;
        public string SegmentChangeCount__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SubmittedDate__c;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ApprovedRejectedDate__c;
        public string Comment__c;
    }
}
