using System;
using System.Collections.Generic;
using System.Linq;

namespace OMREngineTest.Utils
{

    
    public class GeoAlignmentQueryResult : QueryResult
    {
        public const string query_select = "select t.\"UniqueIntegrationId\" \"TerritoryUID\", g.\"UniqueIntegrationId\" \"GeographyUID\""
            + ", act.\"EffectiveDate\", act.\"EndDate\"";
        public const string query_from = " from \"{{schema}}\".\"OMGeographyTerritory\" act"
            + " join \"{{schema}}\".\"OMTerritory\" t on t.\"SOMTerritoryId\" = act.\"SOMTerritoryId\""
            + " join static.\"OMGeography\" g on g.\"SOMGeographyId\" = act.\"SOMGeographyId\"";
        public const string query_order = " order by t.\"UniqueIntegrationId\", g.\"UniqueIntegrationId\", act.\"EffectiveDate\"";

        private List<string> geos, terrs;
        private List<string> extra_select_columns;
        private bool include_inactive; 

        public GeoAlignmentQueryResult(string territoryUID, string geographyUID = null, string expected = null, string extra_columns = null, bool show_inac = false)
            : base(query: "Pending query generation",
                  exp_result: expected,
                  odataQuery: null
                  )
        {
            geos = geographyUID.Split(',').ToList();
            terrs = territoryUID.Split(',').ToList();
            extra_select_columns = extra_columns == null ? new List<string> { } : extra_columns.Split(',').ToList();
            include_inactive = show_inac;
        }

        new public string GetQuery(string act_schema)
        {
            string select_extra = extra_select_columns.Count == 0 ? "" : (", act.\"" + string.Join("\", act.\"", extra_select_columns) + "\"");
            query = query_select + select_extra + (include_inactive ? ", act.\"Status\"" : "")
                  + query_from
                  + " where t.\"UniqueIntegrationId\" in ('" + String.Join("','", terrs) + "')"
                  + (geos is null ? "" : " and g.\"UniqueIntegrationId\" in ('" + String.Join("','", geos) + "')") 
                  + (include_inactive ? "" : " and act.\"Status\" = 'ACTV'")
                  + query_order + ";";
            return query.Replace("{{schema}}", act_schema);
        }
        new public string GetOdataReq()
        {
            return null;
        }
        public override string GetSfQuery(string sf_prefix)
        {
            return null;
        }

    }

}