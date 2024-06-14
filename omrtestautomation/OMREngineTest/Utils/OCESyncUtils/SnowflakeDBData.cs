using MainFramework.GlobalUtils;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit.Abstractions;

namespace OCESyncTest.Utils
{
    public class SnowflakeDBData
    {
        private SnowFlakeHelper _snowflake;
        private readonly ITestOutputHelper _output;
        public IDbConnection conn;
        public SnowflakeDBData(ITestOutputHelper output)
        {
            _output = output;
            _snowflake = new SnowFlakeHelper();
            conn = new SnowflakeDbConnection();
        }

        public Dictionary<string, object> get_TableWise_Data(string strQuery,string snowFlakeConnStr)
        {
            IDataReader reader = _snowflake.CONN_EXEC_ToSnowflake(snowFlakeConnStr, strQuery);
            Dictionary<string, object> dictSnowDb = serialize_Data_Dictionary(reader);

            return dictSnowDb;

        }

        public string get_TableWise_Data(string strQuery, string columnName,string snowFlakeConnStr)
        {
            IDataReader reader = _snowflake.CONN_EXEC_ToSnowflake(snowFlakeConnStr, strQuery);

            string columnNameValue = string.Empty;
            int count = 0;
            while (reader.Read())
            {
                columnNameValue = reader[columnName].ToString();
                _output.WriteLine(reader[count].ToString());
                count++;
                _output.WriteLine(count.ToString());
            }
            return columnNameValue;

        }

        public Dictionary<string, object> serialize_Data_Dictionary(IDataReader reader)
        {
            SnowFlakeHelper snowFlakeHelper = new SnowFlakeHelper();
            var results = new Dictionary<string, object>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));
            while (reader.Read())
            {
                results = snowFlakeHelper.SerializeRow(cols, reader);
            }
            return results;
        }


        public HashSet<String> get_TableWise_DataCount(string strQuery,string snowFlakeConnStr)
        {
            IDataReader reader = _snowflake.CONN_EXEC_ToSnowflake(snowFlakeConnStr, strQuery);

            string columnNameValue = string.Empty;

            HashSet<String> lstTerritories = new HashSet<String>();
            while (reader.Read())
            {
                lstTerritories.Add(reader[2].ToString());
            }

            return lstTerritories;
        }
    }
}
