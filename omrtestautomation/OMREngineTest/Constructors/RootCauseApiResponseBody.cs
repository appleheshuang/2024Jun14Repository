using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.Constructors
{
    public class RootCauseApiResponseBody
    {
        public string omAccountId;
        public string somTerritoryId;
        public string effectiveDate;
        public string endDate;
        public List<RootCauseRule> rules;
    }

    public class RootCauseRule
    {
        public string ruleName;
        public string somRuleId;
        public string uniqueIntegrationId;
        public string effectiveDate;
        public string endDate;
        //[JsonIgnore]
        public string effectiveCalculationDate;// = DateTime.Now.ToString("dd-MM-yyyy");
        //[JsonIgnore]
        public string endCalculationDate;// = DateTime.Now.ToString("dd-MM-yyyy");
        public List<RootCauseData> data;
    }


    public class RootCauseData
    {
        public string table;
        public string id;
        public string uniqueIntegrationId;
        public string column;
        public string beforeEffective;
        public string afterEffective;
        public string beforeEnd;
        public string afterEnd;
    }
}
