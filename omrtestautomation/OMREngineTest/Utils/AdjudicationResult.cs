using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OMREngineTest.Utils
{

    /// <summary>
    /// AdjUserAssignment: Builds a query to test OMAdjudicationUserAssignment results for a given adjudication
    /// </summary>
    public class AdjUserAssignment : QueryResult
    {
        public const string snow_query = "select t.\"UniqueIntegrationId\" \"Territory\", \"Role\", aua.\"Status\", \"SOMUserId\""
            + " from STATIC.\"OMAdjudicationUserAssignment\" aua"
            + " join {{schema}}.\"OMTerritory\" t on t.\"SOMTerritoryId\" = aua.\"SOMTerritoryId\""
            + " where aua.\"OMAdjudicationId\" = '{{adjudicationid}}'"
            + " order by t.\"UniqueIntegrationId\", \"Role\"";

        public AdjUserAssignment(string adjudicationid, string expected)
            : base(query: snow_query.Replace("{{adjudicationid}}", adjudicationid),
                  exp_result: expected,
                  sfQuery: "skip"
                  )
        {
        }

    }

    /// <summary>
    /// AdjudicationResult: Builds a query to test OMXXXAdjudicationResult results for a given entity
    /// Enitity is Account,Geography,Territory
    /// </summary>
    public class AdjudicationResult : QueryResult
    {
        const string selectQuery = "select e.\"UniqueIntegrationId\" \"{{entity}}\", {{columns}}"
            + " from static.\"OMAdjudicationTerritory\" ad"
            + " join {{schema}}.\"OMTerritory\" t on t.\"SOMTerritoryId\" = ad.\"SOMTerritoryId\""
            + " join {{qualifiedadjtable}} ar on ar.\"OMAdjudicationId\" = ad.\"OMAdjudicationId\""
            + "  and ar.\"SOMTerritoryId\" = ad.\"SOMTerritoryId\" and ar.\"AccessKey\" = ad.\"AccessKey{{role}}\""
            + " join {{qualifiedtablename}} e on e.\"{{entityidcol}}\" = ar.\"{{adjentityidcol}}\""
            + " where ad.\"OMAdjudicationId\" = '{{adjudicationid}}'";
        const string pickEntities = " and e.\"UniqueIntegrationId\" in ('{{entityId}}')";
        const string pickTerritories = " and t.\"UniqueIntegrationId\" in ('{{territoryId}}')"; 
        const string orderBy = " order by e.\"UniqueIntegrationId\"";

        public AdjudicationResult(string entityName, string adjTable, string adjEntityIdCol, string entityTable, string entityIdCol, string adjudicationid, string entityUId, string columns, string expected,string role,string territoryUId,string replaceNullWith=null)
            : base(query: "Pending query generation",
                  exp_result: expected,
                  sfQuery: "skip"
                  )
        {
            query = selectQuery.Replace("{{entity}}", entityName)
                  .Replace("{{columns}}", nvlColumns(columns, replaceNullWith, "ar"))
                  .Replace("{{qualifiedadjtable}}", adjTable)
                  .Replace("{{qualifiedtablename}}", entityTable)
                  .Replace("{{entityidcol}}", entityIdCol)
                  .Replace("{{adjentityidcol}}", adjEntityIdCol)
                  .Replace("{{adjudicationid}}", adjudicationid)
                  .Replace("{{role}}",AdjRoleType(role))
                + ( territoryUId == null ? "" : pickTerritories.Replace("{{territoryId}}", string.Join("','", territoryUId.Split(','))) )
                + ( entityUId == null ? "" : pickEntities.Replace("{{entityId}}", string.Join("','", entityUId.Split(','))) )
                + orderBy;
        }

        string AdjRoleType(string roleWanted)
        {
            switch (roleWanted.ToUpper().Substring(0, 2)) {
                case "RE": return "Rep"; 
                case "AP": return "Approver"; 
                case "DM": return "Approver"; 
                case "HE": return "HomeOffice"; 
                case "HO": return "HomeOffice"; 
                default: return "Rep"; 
            }
        }

    }

}