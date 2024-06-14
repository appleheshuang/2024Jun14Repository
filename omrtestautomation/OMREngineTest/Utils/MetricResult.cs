using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OMREngineTest.Utils
{

    /// <summary>
    /// MetricSummaryResult: Builds a query to test ScenarioSummary2 results for a given territory
    /// </summary>
    public class MetricSummaryResult : QueryResult
    {
        public const string snow_query = "select \"TerritoryUniqueIntegrationId\" \"Territory\", \"AccountType\", {{columns}}"
            + " from \"{{schema}}\".\"OMScenarioSummary2\""
            + " where \"OMScenarioId\" = '{{scenarioid}}' and \"TerritoryUniqueIntegrationId\" = '{{territory}}' and \"AccountType\" in ({{accounttypes}})"
            + " order by \"AccountType\"";

        public const string oldtable = "OMScenarioSummary";
        public const string newtable = "OMScenarioSummary2";
        int display_precision;

        const string odata_select = "/OMScenarioSummary2?$select=TerritoryUniqueIntegrationId,AccountType,{{columns}}"
            + "&$filter=OMScenarioId eq '{{scenarioid}}' and TerritoryUniqueIntegrationId eq '{{territory}}' and AccountType in ({{accounttypes}})"
            + "&$orderBy=AccountType asc";

        public MetricSummaryResult(string scenarioid, string acctTypes, string territoryUId, string columns, string expected, int precision, bool useoldmethod = false)
            : base(query: snow_query.Replace("{{columns}}", "\"" + string.Join("\",\"", columns.Split(',').ToList()) + "\"")
                  .Replace("{{scenarioid}}", scenarioid)
                  .Replace("{{territory}}", territoryUId)
                  .Replace("{{accounttypes}}", "'" + string.Join("','", acctTypes.Split(',').ToList()) + "'")
                  .Replace(newtable, useoldmethod ? oldtable : newtable),
                  exp_result: expected,
                  odataQuery: odata_select.Replace("{{columns}}", columns)
                  .Replace("{{scenarioid}}", scenarioid)
                  .Replace("{{territory}}", territoryUId)
                  .Replace("{{accounttypes}}", "'" + string.Join("','", acctTypes.Split(',').ToList()) + "'")
                  .Replace(newtable, useoldmethod ? oldtable : newtable),
                  sfQuery: "skip"
                  )
        {
            display_precision = precision;
        }

        new public string ParseOdata(string response)
        {
            JObject joresp = JObject.Parse(response);
            string as_colname = joresp["value"].ToString().Replace("TerritoryUniqueIntegrationId", "Territory");
            return removeJSONWhitespaces(DecimalPrecision(as_colname, display_precision, "Metric"));
        }
    }

    /// <summary>
    /// MetricResult: Builds a query to test EntityMetricYyy results for a given entity
    /// Enitity is Account,Geography,Territory
    /// Yyy is Before,After,blank
    /// </summary>
    public class MetricResult : QueryResult
    {
        string selectQuery, joinQuery;

        const string select = "select t.\"UniqueIntegrationId\" \"{{entity}}\", {{columns}}";
        const string innerQuery = " from {{qualifiedmetrictable}} m "
            + " join {{qualifiedtablename}} t on t.\"{{entityidcol}}\" = m.\"{{metricentityidcol}}\""
            + " where \"SOMSalesForceId\" = '{{salesforceid}}' and t.\"UniqueIntegrationId\" in ('{{entityId}}')";
        const string outerQuery = " from {{qualifiedtablename}} t "
            + " left outer join {{qualifiedmetrictable}} m on m.\"SOMSalesForceId\" = '{{salesforceid}}'"
            + " and m.\"{{metricentityidcol}}\" = t.\"{{entityidcol}}\""
            + " where t.\"UniqueIntegrationId\" in ('{{entityId}}')";
        const string orderby = " order by t.\"UniqueIntegrationId\"";

        public MetricResult(string entityName, string metricTable, string metricEntityIdCol, string entityTable, string entityIdCol, string salesforceSOMId, string entityId, string columns, string expected,string replaceNullWith=null)
            : base(query: "Pending query generation",
                  exp_result: expected,
                  sfQuery: "skip"
                  )
        {
            if (replaceNullWith != null)
            {
                selectQuery = select.Replace("{{columns}}", nvlColumns(columns, replaceNullWith, "m"));
                joinQuery = outerQuery;
            }
            else
            {
                selectQuery = select.Replace("{{columns}}", "m.\"" + string.Join("\",m.\"", columns.Split(',')) + "\"");
                joinQuery = innerQuery;
            }
            query = selectQuery.Replace("{{entity}}", entityName) 
                + joinQuery.Replace("{{qualifiedmetrictable}}", metricTable)
                  .Replace("{{qualifiedtablename}}", entityTable)
                  .Replace("{{entityidcol}}", entityIdCol)
                  .Replace("{{metricentityidcol}}", metricEntityIdCol)
                  .Replace("{{salesforceid}}", salesforceSOMId)
                  .Replace("{{entityId}}", string.Join("','", entityId.Split(',')))
                + orderby;
        }

    }

    public class EmptyResult : MetricResult
    {
        string[] column_names; string[] ids; string entity;
        public EmptyResult(string entityName, string metricTable, string metricEntityIdCol, string entityTable, string entityIdCol, string salesforceSOMId, string entityId, string columns)
            : base(entityName, metricTable, metricEntityIdCol, entityTable, entityIdCol, salesforceSOMId, entityId, columns, expected: "Pending results generation")
        {
            column_names = columns.Split(',');
            ids = entityId.Split(',');
            entity = entityName;
        }

        new public string GetResult()
        {
            string columns = string.Join(":0.000,", column_names) + ":0.000";
            return "[{"+ entity + ":" + string.Join("," + columns + "},{" + entity + ":", ids) + "," + columns + "}]" ;
        }
    }

}