using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects
{
    public class AdjudicationChangeRequestGeography
    {
        [JsonIgnore]
        public string Id;
        public string OMAdjudicationId__c;
        public string SOMTerritoryId__c;
        public string SOMGeographyId__c;
        public string OMGeographyId__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string RefSalesForceName__c;
        public string GeographyUniqueIntegrationId__c;
        public string TerritoryUniqueIntegrationId__c;
        public string Action__c;
        public string SubmitStatus__c;
        public string ApprovalStatus__c;
        public string ReviewLevel__c;
        public string OMScenarioId__c;
        public string InitialLevel__c;
        public string SimulationStatusRep__c;
        public string SimulationStatusApprover__c;
        public string SubmittedDate__c;
        public string ApprovedRejectedDate__c;
        public string Comment__c;
    }
}
