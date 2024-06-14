using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors.SalesforceObjects.AdjudicationObjects
{
    class AdjudicationTerritory
    {
        [JsonIgnore]
        public string Id; //Required
        public string OMAdjudicationId__c; //Required
        public string SOMTerritoryId__c; //Required
        public string AccessKeyRep__c;
        public string LastSimulatedRep__c;
        public string SimulationStatusRep__c;
        public string AccessKeyApprover__c;
        public string LastSimulatedApprover__c;
        public string SimulationStatusApprover__c;
        public string RefTerritoryName__c;
        public string RefTerritoryType__c;
        public string RefSalesForceName__c;
        public string TerritoryUniqueIntegrationId__c;
        public string UserAssignmentType__c;
        public string SimulatedByRepId__c;
        public string SimulatedByApproverId__c;
        public string SalesForceUniqueIntegrationId__c;
        public bool Generated__c;
    }
}
