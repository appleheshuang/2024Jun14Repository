namespace OMREngineTest.Utils
{

    public class ValidationError : QueryResult
    {
        private const string scenario_error_query = "with \"act_result\" as ("
                + " select \"TableName\",\"PKey\",\"ErrorCode\", \"ErrorMessage\","
                + " contains(\"ErrorMessage\", exp.\"item1\") and contains(\"ErrorMessage\", exp.\"item2\") as \"items_match\","
                + " startswith(\"ErrorMessage\", exp.\"msg\") as \"message_match\","
                + " exp.\"msg\" \"exp_msg\", exp.\"item1\" \"exp_item1\", exp.\"item2\" \"exp_item2\","
                + " substr(\"ErrorMessage\",\"item1_start\", \"item1_end\" - \"item1_start\" + 1) as \"act_item1\","
                + " substr(\"ErrorMessage\",\"item2_start\", \"item2_end\" - \"item2_start\" + 1) as \"act_item2\","
                + " substr(\"ErrorMessage\", 0, \"item1_start\" - 1) as \"act_msg\""
                + " from ("
                + " select position('\"',\"ErrorMessage\") as \"item1_start\", position(']',\"ErrorMessage\") as \"item1_end\", "
                + " position('\"',\"ErrorMessage\",position('] and ',\"ErrorMessage\")) as \"item2_start\", position(']',\"ErrorMessage\",position('] and ',\"ErrorMessage\")+length('] and ')) as \"item2_end\","
                + " \"ErrorMessage\",\"ErrorCode\",\"TableName\",\"PKey\""
                + " from \"{{schema}}\".\"OMScenarioError\""
                + " where \"TableName\"='{{table_name}}'"
                + " {{optional_pkey_match}}"
                + " ) as act,"
                + " ( select '{{exp_message}}' as \"msg\",'{{exp_item1}}' as \"item1\", '{{exp_item2}}' as \"item2\" ) as exp "
            + ")"
            + " select \"TableName\"{{pkey_col}},\"ErrorCode\","
                + " case when \"message_match\" then \"exp_msg\" else \"act_msg\" end as \"ErrorMessage\","
                + " case when \"items_match\" then \"exp_item1\" else \"act_item1\" end as \"Item1\","
                + " case when \"items_match\" then \"exp_item2\" else \"act_item2\" end as \"Item2\""
                + " from \"act_result\""
                + " order by \"items_match\" desc, \"message_match\" desc limit 1";

        private const string simple_error_query = "select \"TableName\"{{pkey_col}},\"ErrorCode\","
                + " case when startswith(\"ErrorMessage\", '{{exp_message}}') then '{{exp_message}}' else \"ErrorMessage\" end as \"ErrorMessage\""
                + " from \"{{schema}}\".\"OMScenarioError\""
                + " where \"TableName\"='{{table_name}}'"
                + " {{optional_pkey_match}}"
            + " order by \"TableName\", \"PKey\" desc limit 1";

        /// <summary>
        /// Match error for the table, primary key and entities involved in the error message
        /// Used when there is an preprocessing error on a mapping table (2 parent FKs)
        /// Note: The order of the entities in the error message text does not matter (and can change)
        /// </summary>
        /// <param name="table">Table the error is for</param>
        /// <param name="exp_code">Error code</param>
        /// <param name="exp_message">Error text (excluding entity info)</param>
        /// <param name="exp_item1">UniqueIntgrationId for parent item</param>
        /// <param name="exp_item2">UniqueIntgrationId for other parent item</param>
        /// <param name="somid">Id for the PKey in the error details (optional)</param>
        public ValidationError(string table, string exp_code, string exp_message, string exp_item1, string exp_item2, string somid = null)
            : base(query: scenario_error_query.Replace("{{exp_message}}", exp_message)
                  .Replace("{{table_name}}", table)
                  .Replace("{{exp_item1}}", exp_item1)
                  .Replace("{{exp_item2}}", exp_item2)
                  .Replace("{{pkey_col}}", somid == null ? "" : ",\"PKey\"")
                  .Replace("{{optional_pkey_match}}", somid == null ? "" : "and \"PKey\" = '" + somid + "'"),
                exp_result: "[{TableName:" + table + (somid == null ? "" : ",PKey:" + somid) + ",ErrorCode:" + exp_code + ",ErrorMessage:" + exp_message + ",Item1:" + exp_item1 + ",Item2:" + exp_item2 + "}]",
                sfQuery: "skip")
        {
        }

        /// <summary>
        /// Match error for the table, primary key. Ignore entity information in the error message
        /// </summary>
        /// <param name="table">Table the error is for</param>
        /// <param name="exp_code">Error code</param>
        /// <param name="exp_message">Error text (excluding entity info)</param>
        /// <param name="somid">Id for the PKey in the error details (optional)</param>
        public ValidationError(string table, string exp_code, string exp_message, string somid = null)
            : base(query: simple_error_query.Replace("{{exp_message}}", exp_message)
                  .Replace("{{table_name}}", table)
                  .Replace("{{pkey_col}}", somid == null ? "" : ",\"PKey\"")
                  .Replace("{{optional_pkey_match}}", somid == null ? "" : "and \"PKey\" = '" + somid + "'"),
                exp_result: "[{TableName:" + table + (somid == null ? "" : ",PKey:" + somid) + ",ErrorCode:" + exp_code + ",ErrorMessage:" + exp_message + "}]",
                sfQuery: "skip")
        {
        }
    }

}