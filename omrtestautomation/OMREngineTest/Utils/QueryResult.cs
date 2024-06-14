using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework.GlobalUtils;

namespace OMREngineTest.Utils
{

    public class QueryResult
    {
        public string query { get; set; }
        public string exp_result { get; set; }
        public string odata_req { get; set; }
        public string sfquery { get; set; }

        public QueryResult(string query, string exp_result = "[]", string odataQuery = null, string sfQuery = null)
        {
            string queryFirstWord = query.Split(' ')[0].ToLower();
            List<string> sqlKeywords = new List<string>{ "select", "insert", "update", "with" };
            string _sql;
            if (query == "skip") _sql = query;
            else if (sqlKeywords.Contains(queryFirstWord))
            {
                _sql = query;
            }
            else
            {
                _sql = BuildQuery(query, "{{schema}}");
            }
            this.query = _sql;
            this.exp_result = removeJSONWhitespaces(exp_result);
            odata_req = odataQuery;
            sfquery = sfQuery;
        }

        public void AddResult(string add_expected)
        {
            string trimmed_curr = exp_result == "[]" ? "[" : exp_result.Substring(0, exp_result.Length - 1) + ",";
            exp_result = trimmed_curr + "{" + add_expected + "}]";
        }

        public virtual string GetQuery(string act_schema)
        {
            return query.Replace("{{schema}}", act_schema).Replace("{{inputschema}}", act_schema.Replace("OUTPUT","INPUT"));
        }
        public virtual string GetSfQuery(string sf_prefix)
        {
            string _sfquery;
            if (sfquery == null)
            { //replace with generated query if not already set
                _sfquery = query.Replace("\"{{schema}}\".", "");
                _sfquery = _sfquery.Replace(" \"", " " + sf_prefix);
                _sfquery = _sfquery.Replace(",\"", ", " + sf_prefix);
                _sfquery = _sfquery.Replace("\"", "__c");
                _sfquery = _sfquery.Replace(sf_prefix + "Id__c", "Id");
                _sfquery = _sfquery.Replace(sf_prefix + "Name__c", "Name");
                _sfquery = _sfquery.Trim(';');
            }
            else
            {
                _sfquery = sfquery.Replace("{{prefix}}", sf_prefix);
            }
            return _sfquery;
        }
        public virtual string GetOdataReq()
        {
            if ((odata_req ?? "").Contains("as-of-date=")) {
                int datetime_start = odata_req.IndexOf("as-of-date=") + "as-of-date=".Length;
                string orig_asofdate = "as-of-date=" + odata_req.Substring(datetime_start, "yyyy-mm-dd hh:mm:ss stz:00".Length);
                string new_asofdate = "as-of-date=" + ReplaceTzWithAsciiInDateTime(odata_req.Substring(datetime_start, "yyyy-mm-dd hh:mm:ss stz:00".Length));
                return odata_req.Replace(orig_asofdate, new_asofdate);
            }
            else
                return odata_req;
        }

        public string GetResult()
        {
            return exp_result;
        }
        public string OdataExpResult()
        {
            return exp_result.Replace("T00:00:00Z", "");
        }
        public virtual string SfExpResult()
        {
            return exp_result.Replace("T00:00:00Z", "");
        }
        public virtual string ParseSnowdata(string response)
        {
            return removeJSONWhitespaces(response).Replace("T00:00:00Z", "");

        }

        private string ReplaceTzWithAsciiInDateTime(string orig_datetime)
        {
            int timezone_start = "yyyy-mm-dd hh:mm:ss ".Length;
            string tz_sign = orig_datetime.Substring(timezone_start, 1);
            string tz_sign_ascii = BitConverter.ToString(new byte[] { Convert.ToByte(tz_sign[0]) });
            string new_tz = orig_datetime.Substring(timezone_start, "stz:00".Length).Replace(tz_sign, '%' + tz_sign_ascii);
            return orig_datetime.Substring(0, timezone_start) + new_tz;
        }
        private String BuildQuery(String input, string db_schema)
        {
            string query;
            string Condition = string.Empty;
            string[] columns = input.Split(",");
            string tableName = "\"" + db_schema + "\".\"" + columns[0] + "\"";
            if (columns[0].Contains('.'))
            {
                string[] schema_table = columns[0].Split('.');
                tableName = "\"" + schema_table[0].ToUpper() + "\".\"" + schema_table[1] + "\"";
            }
            string SelectColumnNames = string.Empty;
            for (int j = 1; j < columns.Length; j++)
            {
                if (!columns[j].Contains(":"))
                {
                    if (SelectColumnNames != String.Empty)
                    {
                        SelectColumnNames += ",";
                    }
                    SelectColumnNames += "\"" + columns[j] + "\"";
                }
                else
                {
                    string[] conditionstring = columns[j].Split(":");
                    if (conditionstring[1].Contains("|"))
                    {
                        string[] conditionStringNew = conditionstring[1].Split("|");
                        string conditionStringCombile = string.Empty;
                        for (int m = 0; m < conditionStringNew.Length; m++)
                        {
                            conditionStringCombile = conditionStringNew[m] + "," + conditionStringCombile;
                        }
                        while (conditionStringCombile.EndsWith(","))
                            conditionStringCombile = conditionStringCombile.Substring(0, conditionStringCombile.Length - 1) + " ";
                        Condition = "\"" + conditionstring[0] + "\" in (" + conditionStringCombile + ")" + " and " + Condition;
                    }
                    else
                    {
                        Condition = "\"" + conditionstring[0] + "\"=" + conditionstring[1] + " and " + Condition;
                    }
                }

                while (Condition.EndsWith(" and "))
                    Condition = Condition.Substring(0, Condition.Length - 5) + "";
            }
            if (String.IsNullOrEmpty(Condition))
            {
                query = "Select " + SelectColumnNames + " from " + tableName + ";";
            }
            else
            {
                query = "Select " + SelectColumnNames + " from " + tableName + " where " + Condition + ";";
            }
            return query;
        }

        public string AddFilterValue(string orig_filter, string table_prefix, string filter_value)
        {
            string filter_column = table_prefix + "\"UniqueIntegrationId\"";
            if (orig_filter.Contains(filter_column))
            {
                int filter_pos = orig_filter.IndexOf(filter_column);
                int insert_pos = orig_filter.IndexOf(")", filter_pos);
                return orig_filter.Insert(insert_pos, ",'" + filter_value + "'");
            }
            else
                return orig_filter.Insert(orig_filter.Length - 1, " and " + table_prefix + "\"UniqueIntegrationId\" in ('" + filter_value + "')");
        }
        public void ModifyQuery(string filter_value, string filter_table = null)
        {
            string table_prefix = filter_table is null ? "" : filter_table + ".";
            string orig_query = query;
            string query_filter;
            int where_start = orig_query.IndexOf("where");
            int where_end = orig_query.IndexOf("order", where_start);
            int end = orig_query.Length;
            if (where_start > 0)
            {
                query_filter = AddFilterValue(orig_query.Substring(where_start, where_end - where_start), table_prefix, filter_value);
            }
            else
            {
                where_start = where_end;
                query_filter = "where " + table_prefix + "\"UniqueIntegrationId\" in ('" + filter_value + "')";
            }
            query = orig_query.Substring(0, where_start) + " " + query_filter + " " + orig_query.Substring(where_end, end - where_end);
        }
        public string ParseOdata(string response)
        {
            JObject joresp = JObject.Parse(response);
            JArray jaValue = JArray.Parse(joresp["value"].ToString());
            return removeJSONWhitespaces(jaValue.ToString());
        }

        public string DecimalPrecision(string result, int dp, string column_match, string empty_string ="null")
        {
            string decimal_format = "0.000000".Substring(0, dp + 2);
            JArray jaValue = JArray.Parse(result);
            JArray parsed = new JArray() { };
            foreach (JObject res in jaValue)
            {
                Dictionary<string,string> new_res = new Dictionary<string, string>() { };
                foreach (KeyValuePair<string, JToken> col in res)
                {
                    string new_val;
                    if (col.Key.StartsWith(column_match) || column_match == null || column_match == "")
                        try
                        {
                            decimal val = decimal.Parse(col.Value.ToString());
                            new_val = val.ToString(decimal_format);
                        }
                        catch
                        {
                            new_val = col.Value.ToString() ?? "";
                            if (new_val == "") new_val = empty_string;
                        }
                    else
                        new_val = col.Value.ToString();
                    new_res.Add(col.Key,new_val);
                }
                parsed.Add(JObject.FromObject(new_res));
            }
            return parsed.ToString();
        }

        public string removeJSONWhitespaces(string jsonValue)
        {
            jsonValue = jsonValue.Replace("\"", "");
            jsonValue = jsonValue.Replace("\r", "");
            jsonValue = jsonValue.Replace("\n", "");
            jsonValue = jsonValue.Replace("\t", "");
            jsonValue = jsonValue.Replace(": ", ":");
            jsonValue = jsonValue.Replace("    ", "");
            jsonValue = jsonValue.Replace("  ", "");
            jsonValue = jsonValue.Replace("\\", "");

            return jsonValue;
        }
        private string format_value(KeyValuePair<string, JToken> node)
        {
            // Any re-formatting required for the sf response value string to match expected
            if (node.Key.Contains("HQGoal"))
                return node.Value.ToString().Replace(".0", "");
            else if (node.Key.Contains("Territories")) //trim the leading and trailing ;
                return node.Value.ToString().TrimStart(';').TrimEnd(';');
            else
                return node.Value.ToString();
        }
        private JObject flattened_node(JObject node, JObject parsed_so_far)
        {
            node.Remove("attributes");
            foreach (KeyValuePair<string, JToken> sub_node in node)
            {
                try
                {
                    JObject child_node = (JObject)sub_node.Value; // if this is a sub-node then flatten object references
                    JObject parsed_child_node = flattened_node(child_node, new JObject());
                    foreach (KeyValuePair<string, JToken> root_node in parsed_child_node)
                        parsed_so_far.Add(root_node.Key, format_value(root_node));
                }
                catch
                {
                    parsed_so_far.Add(sub_node.Key, format_value(sub_node));
                }
            }
            return parsed_so_far;
        }
        public virtual string ParseSFdata(JArray all_results, string ns)
        {
            List<string> strValues = new List<string>() { };
            foreach (JObject res in all_results)
            {
                JObject sfValue = flattened_node(res, new JObject());
                strValues.Add(sfValue.ToString(Formatting.None));
            }
            string prefix = ns.EndsWith('_') ? ns : ns + "__";
            return '[' + removeJSONWhitespaces(string.Join(',', strValues.ToArray()).Replace(prefix, "").Replace("__c", "")) + ']';
        }

        /// <summary>
        /// Replace a comma separated list of column names with the query ready list.
        /// Replace null values with a default value when given
        /// If a column is already aliased, use that instead of the default tableAlias provided
        /// </summary>
        /// <param name="columns">comma seperated list of columns to include in select</param>
        /// <param name="nvlVal">Value to reaplce nulls with. No replacement if null</param>
        /// <param name="tableAlias">Table alias to prefix columns with</param>
        /// <returns></returns>
        public string nvlColumns(string columns, string nvlVal, string tableAlias)
        {
            KeyValuePair<string,string>[] tableCols = columns.Split(',').Select(col =>       
                col.Contains('.') 
                    ? new KeyValuePair<string,string>(col.Split('.')[1], col.Split('.')[0] + ".\"" + col.Split('.')[1] + "\"")
                    : new KeyValuePair<string,string>(col, tableAlias + ".\"" + col + "\"")
                ).ToArray();
            string[] nvlCols;
            if (nvlVal == null)
                nvlCols = tableCols.Select(i => i.Value).ToArray();
            else
                nvlCols = tableCols.Select(i => "nvl(" + i.Value + ",'" + nvlVal + "') as \"" + i.Key + "\"").ToArray();
            return string.Join(',', nvlCols);
        }

    }

    public class SummaryQueryResult : QueryResult
    {
        const string query_select = "select \"TerritoryUniqueIntegrationId\", \"AccountType\""
            + ", \"AccountsGained\", \"AccountsLost\", \"GeographiesGained\", \"GeographiesLost\""
            + ", \"AccountsTotal\", \"GeographiesTotal\"";
        const string query_from = " from \"{{schema}}\".\"OMScenarioSummary\"";
        const string query_order = " order by \"TerritoryUniqueIntegrationId\", \"AccountType\"";

        public SummaryQueryResult(string scenarioID, string _territoryUID, string account_type,
            int acc_gain, int acc_lost, int geo_gain, int geo_lost, int acc_tot, int geo_tot)
            : base(query: query_select + query_from + " where \"OMScenarioId\" = '" + scenarioID + "'"
                + " and \"TerritoryUniqueIntegrationId\" in ('" + _territoryUID + "')"
                + " and \"AccountType\" in ('" + account_type + "')" + query_order + ";",
                  exp_result: "[{TerritoryUniqueIntegrationId:" + _territoryUID + ",AccountType:" + account_type
                + ",AccountsGained:" + acc_gain + ",AccountsLost:" + acc_lost + ",GeographiesGained:" + geo_gain
                + ",GeographiesLost:" + geo_lost + ",AccountsTotal:" + acc_tot + ",GeographiesTotal:" + geo_tot + "}]",
                  odataQuery: "/OMScenarioSummary?$select=TerritoryUniqueIntegrationId,AccountType,AccountsGained,AccountsLost,GeographiesGained,GeographiesLost"
                  + ",AccountsTotal,GeographiesTotal&$filter=OMScenarioId eq '" + scenarioID + "' AND TerritoryUniqueIntegrationId eq '" + _territoryUID
                  + "'&$orderBy=TerritoryUniqueIntegrationId,AccountType")
        {
        }
        public SummaryQueryResult(string scenarioID)
            : base(query: query_select + query_from + " where \"OMScenarioId\" = '" + scenarioID + "'" + query_order + ";",
                  exp_result: "[]",
                  odataQuery: "/OMScenarioSummary?$select=TerritoryUniqueIntegrationId,AccountType,AccountsGained,AccountsLost,GeographiesGained,GeographiesLost"
                  + ",AccountsTotal,GeographiesTotal&$filter=OMScenarioId eq '" + scenarioID
                  + "'&$orderBy=TerritoryUniqueIntegrationId,AccountType")
        {
        }
        public void AddSummaryResult(string _territoryUID, string account_type, int acc_gain, int acc_lost, int geo_gain, int geo_lost, int acc_tot, int geo_tot)
        {
            string new_result = "TerritoryUniqueIntegrationId:" + _territoryUID + ",AccountType:" + account_type
                + ",AccountsGained:" + acc_gain + ",AccountsLost:" + acc_lost + ",GeographiesGained:" + geo_gain
                + ",GeographiesLost:" + geo_lost + ",AccountsTotal:" + acc_tot + ",GeographiesTotal:" + geo_tot;
            AddResult(new_result);
        }
        public override string GetSfQuery(string sf_prefix)
        {
            return null; // Scenario Summary isn't sync'd to sfdc
        }
    }

    public class OCEQueryResult : QueryResult
    {
        string oce_prefix;
        public OCEQueryResult(string ocequery, string exp_result, string oce_namespace, string snow_query = null)
            : base(query: snow_query ?? "skip", exp_result: exp_result, sfQuery: ocequery)
        {
            oce_prefix = oce_namespace.Trim('_') + "__";
        }
        /*public override string GetQuery(string schema)
        {
            return null;
        }*/
        public override string GetOdataReq()
        {
            return null;
        }
        public override string GetSfQuery(string sf_prefix = null)
        {
            return sfquery.Replace("{{prefix}}", oce_prefix);
        }
        public override string ParseSFdata(JArray res, string ns)
        {
            return base.ParseSFdata(res, oce_prefix);
        }
    }

    public class TimestampQueryResult : QueryResult
    {
        private const string default_max = "300";
        private const string db_timestamp = "select current_timestamp as \"exp\" from dual";
        private const string scenario_timestamp = "select \"ProcessingStartTime\" as \"start\", \"ProcessingEndTime\" as \"end\" from static.\"OMScenario\" where \"Id\" = 'ScenarioId'";
        private const string sync_timestamp = "select \"SyncStartTime\" as \"start\", \"SyncEndTime\" as \"end\" from static.\"OMScenario\" where \"Id\" = 'ScenarioId'";
        private const string query_str = "with ts as (compare_query), times_to_check as (test_query) " +
            "select count(*) as \"failure_count\" from ts, times_to_check ";
        private const string within_scenario_dur = "where times_to_check.\"act\" < dateadd(second, -max_elapsed, ts.\"start\") or times_to_check.\"act\" > dateadd(second, max_elapsed, ts.\"end\")";
        private const string within_elapsed_sec = "where abs(timestampdiff(sec,ts.\"exp\",times_to_check.\"act\")) > max_elapsed";
        public const string use_scenario_timestamp = "scenario";

        public TimestampQueryResult(string SQL, string compare_to = null, string max_elapsed = default_max)
            : base(query: (query_str + within_elapsed_sec).Replace("compare_query", compare_to ?? db_timestamp).Replace("test_query", SQL).Replace("max_elapsed",max_elapsed),
                  exp_result: "[{failure_count:0}]")
        {
            if ( !(compare_to == null || compare_to.Contains("exp")) ) throw new Exception("Column named 'exp' must be returned by comparison query. Comparison query=" + compare_to);
            if ( !SQL.Contains("act") ) throw new Exception("Column named 'act' must be returned by query. This query=" + SQL);
        }
        public TimestampQueryResult(string SQL, string ScenarioId, bool sync = false, string max_elapsed = default_max)
            : base(query: (query_str+within_scenario_dur).Replace("compare_query", (sync ? sync_timestamp : scenario_timestamp)).Replace("ScenarioId",ScenarioId).Replace("test_query", SQL).Replace("max_elapsed", max_elapsed),
          exp_result: "[{failure_count:0}]")
        {
            if (!SQL.Contains("act")) throw new Exception("Column named 'act' must be returned by query. This query=" + SQL);
        }
        public override string GetOdataReq()
        {
            return null;
        }
        public override string GetSfQuery(string sf_prefix)
        {
            return null; // Not constructing the sf query equivalent at this stage
        }
    }
    public class QueryHistory : QueryResult
    {
        private const string history_query = "select count(*) \"row_count\" from ("
            + "select query_id, query_tag, start_time, TOTAL_ELAPSED_TIME, rows_produced, error_code, ERROR_MESSAGE, query_type," 
            + " substr(regexp_replace(regexp_replace(query_text, '[\\r\\n]', ' '), '^\\s*', ''),1,150) as Sql"
            + " from table(information_schema.query_history("
            + " end_time_range_start => timestampadd(min,-{{elapsed_min}}, current_timestamp),"
            + " end_time_range_end   => current_timestamp,"
            + " result_limit         => 5000))"
            + " where query_tag = '{{query_tag}}'"
            + "{{with_sql_snippet}}"
            + " order by start_time desc"
            + " limit {{result_count}}"
            + ")";
        private const string with_sql_snippet = " and query_text like '%{{query_text}}%'";

        public QueryHistory(string query_tag, string elapsed_time_min, string row_count, string query_text=null)
            : base(query: history_query.Replace("{{elapsed_min}}", elapsed_time_min).Replace("{{query_tag}}", query_tag)
                .Replace("{{with_sql_snippet}}", query_text is null ? "" : with_sql_snippet.Replace("{{query_text}}", query_text))
                .Replace("{{result_count}}", row_count),
                exp_result: "[{row_count:"+ row_count +"}]")
        {
        }
        public override string GetOdataReq()
        {
            return null;
        }
        public override string GetSfQuery(string sf_prefix)
        {
            return null; 
        }
    }
    public class VolumeResult : QueryResult
    {
        private const string volume_query = "select case when \"row_count\" >= {{threshold}} then 'Threshold met'"
            + " else 'Row count = ' || \"row_count\" || ' does not meet record threshold of {{threshold}}' end as \"Result\" from ("
            + " select count(*) as \"row_count\" from {{table_name}} )";
        public VolumeResult(string table, string min_count)
            : base(query: volume_query.Replace("{{threshold}}", min_count).Replace("{{table_name}}", table),
                exp_result: "[{Result:Threshold met}]")
        { 
        }
    }
    
 
}