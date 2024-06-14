using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework.GlobalUtils;

namespace OMREngineTest.Utils
{

    
    public class AccountMappingQueryResult : QueryResult
    {
        public const string query_select = "select t.\"UniqueIntegrationId\" \"entity_nameUID\", ac.\"UniqueIntegrationId\" \"AccountUID\""
            + ", act.\"EffectiveDate\", act.\"EndDate\"";
        public const string query_from = " from \"{{schema}}\".\"mapping_table\" act"
            + " join \"{{schema}}\".\"OMentity_name\" t on t.\"SOMentity_nameId\" = act.\"SOMentity_nameId\""
            + " join static.\"OMAccount\" ac on ac.\"Id\" = act.\"OMAccountId\"";
        public const string query_order = " order by t.\"UniqueIntegrationId\", ac.\"UniqueIntegrationId\", act.\"EffectiveDate\"";

        const string odata_select = "/mapping_table?$select=EffectiveDate,EndDate&$expand=OMentity_name($select=UniqueIntegrationId),OMAccount($select=UniqueIntegrationId)";
        const string odata_order = "&$orderBy=OMentity_name/UniqueIntegrationId,OMAccountId,EffectiveDate asc";
        public List<string> entities; public List<string> accounts; private List<string> som_entities; private List<string> om_accounts;
        private List<string> extra_select_columns;
        private string mapping_table; private string entity_name; private bool include_inactive; 

        //private readonly ITestOutputHelper _output;

        public AccountMappingQueryResult(string tablename, string entity, string entityUID, string accountUID = null, string expected = null, string _entitySOMId = null, string _acctOMId = null, string extra_columns = null, bool show_inac = false)
            : base(query: "Pending query generation",
                  exp_result: expected,
                  odataQuery: null
                  )
        {
            accounts = accountUID.Split(',').ToList();
            entities = entityUID.Split(',').ToList();
            som_entities = _entitySOMId.Split(',').ToList();
            om_accounts = _acctOMId == null ? null : _acctOMId.Split(',').ToList();
            extra_select_columns = extra_columns == null ? new List<string>{ } : extra_columns.Split(',').ToList();
            mapping_table = tablename;
            entity_name = entity;
            include_inactive = show_inac;
        }
        public void AddAccountFilter(string accountUID, string acctOMId = null)
        {
            accounts.Add(accountUID);
            if (acctOMId != null) om_accounts.Add(acctOMId);
        }
        new public string GetQuery(string act_schema)
        {
            string select_extra = extra_select_columns.Count == 0 ? "" : (", act.\"" + string.Join("\", act.\"", extra_select_columns) + "\"");
            query = query_select + select_extra + (include_inactive ? ", act.\"Status\"" : "")
                  + query_from.Replace("mapping_table", mapping_table)
                  + " where t.\"UniqueIntegrationId\" in ('" + String.Join("','", entities) + "')"
                  + (accounts is null ? "" : " and ac.\"UniqueIntegrationId\" in ('" + String.Join("','", accounts) + "')") 
                  + (include_inactive ? "" : " and act.\"Status\" = 'ACTV'")
                  + query_order + ";";
            return query.Replace("{{schema}}", act_schema).Replace("entity_name", entity_name);
        }
        new public string GetOdataReq()
        {
            return null;
            /* Due to OMR-12174 we are not getting odata response with the correct sort order.
             * Restore this test after that bug is fixed
            string select_extra = extra_select_columns.Count == 0 ? "" : ("," + string.Join(',', extra_select_columns));
            odata_req = som_entities == null ? null : odata_select.Replace("mapping_table", mapping_table).Replace("&$expand", select_extra + "&$expand")
                + "&$filter=contains('" + String.Join(",", som_entities) + "',SOM" + entity_name + "Id)"
                + (om_accounts is null ? "" : " and contains('" + String.Join(",", om_accounts) + "',OMAccountId)") 
                + (include_inactive ? "" : " and Status eq 'ACTV'")
                + odata_order;
            return odata_req.Replace("entity_name", entity_name); */
        }
        public override string GetSfQuery(string sf_prefix)
        {
            return null;
        }

        new public string ParseOdata(string response)
        {
            JObject joresp = JObject.Parse(response);
            JArray jaValue = JArray.Parse(joresp["value"].ToString());
            JArray jaExpected = new JArray();
            foreach (var item in jaValue)
            {
                Dictionary<string, string> expDic = new Dictionary<string, string>();
                JObject terrNode = JObject.Parse(item["OM" + entity_name].ToString());
                JObject acctNode = JObject.Parse(item["OMAccount"].ToString());

                expDic.Add(entity_name + "UID", terrNode["UniqueIntegrationId"].ToString());
                expDic.Add("AccountUID", acctNode["UniqueIntegrationId"].ToString());
                expDic.Add("EffectiveDate", item["EffectiveDate"].ToString());
                expDic.Add("EndDate", item["EndDate"].ToString());
                foreach (string extr_col in extra_select_columns)
                {
                    expDic.Add(extr_col, item[extr_col].ToString());
                }

                string json = JsonConvert.SerializeObject(expDic);
                jaExpected.Add(json);
            }
            return removeJSONWhitespaces(jaExpected.ToString());
        }
    }
    public class AccountTerritoryQueryResult : AccountMappingQueryResult
    {
        public AccountTerritoryQueryResult(string tablename, string _territoryUID, string accountUID = null, string expected = null, string _terrSOMId = null, string _acctOMId = null, string extra_columns = null, bool show_inac = false)
            : base(tablename, "Territory", _territoryUID, accountUID, expected, _terrSOMId, _acctOMId, extra_columns, show_inac)
        {
        }
    }
    public class AlignmentQueryResult : AccountTerritoryQueryResult
    {

        public AlignmentQueryResult(string _territoryUID, string accountUID = null, string expected = null, string _terrSOMId = null, string _acctOMId = null)
            : base(tablename: "OMAccountTerritory", _territoryUID, accountUID, expected, _terrSOMId, _acctOMId,
                  extra_columns: "Source,Reason")
        {
        }
        new public string GetQuery(string act_schema)
        {
            query = query_select + ", act.\"Source\",act.\"Reason\""
                  + query_from.Replace("mapping_table", "OMAccountTerritory")
                  + " where t.\"UniqueIntegrationId\" in ('" + String.Join("','", entities) + "')"
                  + (accounts is null ? "" : " and ac.\"UniqueIntegrationId\" in ('" + String.Join("','", accounts) + "')")
                  + query_order + ";";
            return query.Replace("{{schema}}", act_schema).Replace("entity_name", "Territory");
        }

    }

}