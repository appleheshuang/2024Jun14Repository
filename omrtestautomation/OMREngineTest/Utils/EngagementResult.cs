using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework.GlobalUtils;

namespace OMREngineTest.Utils
{

    
    public class EngagementResult : QueryResult
    {
        public const string query_select = "select t.\"UniqueIntegrationId\" \"TerritoryUID\", ac.\"UniqueIntegrationId\" \"AccountUID\"";
        public const string query_from = " from \"{{schema}}\".\"mapping_table\" act"
            + " join \"{{schema}}\".\"OMTerritory\" t on t.\"SOMTerritoryId\" = act.\"SOMTerritoryId\""
            + " join static.\"OMAccount\" ac on ac.\"Id\" = act.\"OMAccountId\"";
        public const string query_order = " order by t.\"UniqueIntegrationId\", ac.\"UniqueIntegrationId\"";

        public string eng_plan_somid; private string som_territory; 
        public List<string> accounts;
        private List<string> extra_select_columns;
        private string mapping_table;

        public EngagementResult(string tablename, string engplanSOMId, string territorySOMId, string accountUID, string exp_accounts, string exp_territory, string exp_segId, string exp_target)
            : base(query: "Pending query generation"
                  )
        {
            eng_plan_somid = engplanSOMId;
            accounts = accountUID.Split(',').ToList();
            som_territory = territorySOMId;
            extra_select_columns = "SOMEngagementSegmentId,Targets".Split(',').ToList();
            mapping_table = tablename;
            if (exp_territory != null && exp_territory.Length > 0)
                foreach (string acct in exp_accounts.Split(',').ToList())
                    exp_result = AddAccountResult(exp_territory,acct,exp_segId,exp_target);
        }

        string AddAccountResult(string terr_uid, string acct_uid, string segId, string targets)
        {
            string this_result = "{TerritoryUID:" + terr_uid + ",AccountUID:" + acct_uid + ",SOMEngagementSegmentId:" + segId + ",Targets:" + targets + "}";
            return exp_result.Insert(exp_result.Length - 1, (exp_result == "[]" ? "" : ",") + this_result);
        }

        new public string GetQuery(string act_schema)
        {
            string select_extra = extra_select_columns.Count == 0 ? "" : (", act.\"" + string.Join("\", act.\"", extra_select_columns) + "\"");
            query = query_select + select_extra
                  + query_from.Replace("mapping_table", mapping_table)
                  + " where act.\"SOMEngagementPlanId\" = '" + eng_plan_somid + "'"
                  + " and t.\"SOMTerritoryId\" = '" + som_territory + "'"
                  + (accounts is null ? "" : " and ac.\"UniqueIntegrationId\" in ('" + String.Join("','", accounts) + "')") 
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
        new public virtual string ParseSnowdata(string response)
        {
            return removeJSONWhitespaces(response).Replace("T00:00:00Z", "");
        }
        new string removeJSONWhitespaces(string jsonValue)
        {
            jsonValue = jsonValue.Replace("\"", "");
            jsonValue = jsonValue.Replace("\\r", "");
            jsonValue = jsonValue.Replace("\\n", "");
            jsonValue = jsonValue.Replace("\\t", "");
            jsonValue = jsonValue.Replace(": ", ":");
            jsonValue = jsonValue.Replace("    ", "");
            jsonValue = jsonValue.Replace("  ", "");
            jsonValue = jsonValue.Replace("\\", "");

            return jsonValue;
        }
    }
}