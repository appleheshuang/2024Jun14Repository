using Newtonsoft.Json;
using Sailfish.RealignmentEngine.Data;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Xunit;

namespace MainFramework.GlobalUtils
{
    public class QuerySnowflake
    {

        public IDbConnection conn;
        public const int poll_interval = 10;
        public const int default_max_dur = 600;

        public QuerySnowflake(string tenant_snowflake)
        {
            conn = new SnowflakeDbConnection();
            string tempFolder = Path.Combine("temp-resources");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);

            conn.ConnectionString = tenant_snowflake;
            IDataStore snowflake = new SnowflakeDatabase(new DbAccess(conn));
        }

        public bool RunQuery(string q)
        {
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = q;
            try
            {
                cmd.ExecuteReader();
                return true;
            }
            catch { return false; }
        }

        public bool UpdateValue(string tablename, string lookup_clm, string update_value, string known_clm, string known_value, string schema = "OUTPUT")
        {
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            string count_query = "select count(*) \"rec_count\" from \"" + schema + "\".\"" + tablename + "\" where \"" + known_clm + "\" = '" + known_value + "'"; ;
            cmd.CommandText = count_query;
            IDataReader reader = cmd.ExecuteReader();
            reader.Read();
            if (reader["rec_count"].ToString() == "1")
            {
                string update_query = "update \"" + schema + "\".\"" + tablename + "\" set \"" + lookup_clm + "\" = '" + update_value + "'" +
                    " where \"" + known_clm + "\" = '" + known_value + "'";
                cmd.CommandText = update_query;
                cmd.ExecuteReader();
                return true;
            }
            else return false;
        }
        public string LookupValue(string tablename, string lookup_clm, string known_clm, string known_value, string schema = "OUTPUT")
        {
            string query = "select \"" + lookup_clm + "\" from \"" + schema + "\".\"" + tablename + "\" where \"" + known_clm + "\" = '" + known_value + "' limit 1";
            return ReturnSingleColumnValueFromSnowFlake(query, lookup_clm);
        }
        public string GetValuesAsCsv(string tablename, string lookup_clm, string known_clm, string known_values_csv, string schema = "OUTPUT")
        {
            string quoted_values_csv = "'" + known_values_csv.Replace(",", "','") + "'";
            string query_known_values = "select \"" + lookup_clm + "\" from \"" + schema + "\".\"" + tablename + "\" where \"" + known_clm + "\" in (" + quoted_values_csv + ") order by \"" + lookup_clm + "\"";

            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = query_known_values;
            IDataReader reader = cmd.ExecuteReader();
            List<string> values = new List<string>() { };
            while (reader.Read())
                values.Add(reader[lookup_clm].ToString());
            return string.Join(',', values);
        }
        /**
         * Search and get a Column value
         */
        public string ReturnSingleColumnValueFromSnowFlake(string sql, string columnName)
        {
            // Verify in Snowflake
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();
            string columnNameValue = "{not found}";
            int count = 0;
            while (reader.Read())
            {
                columnNameValue = reader[columnName].ToString();
                //_output.WriteLine(reader[count].ToString());
                count++;
                //_output.WriteLine(count.ToString());
            }
            return columnNameValue;
        }

        public bool NotFound(string result)
        {
            return result.Contains("{not found}");
        }

        /**
         * Search and get a Column value
         */
        public DateTimeOffset ReturnSingleDateTimeColumnValueFromSnowFlake(string sql, string columnName)
        {
            // Verify in Snowflake
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();
            DateTimeOffset columnNameValue = DateTimeOffset.Now;
            int count = 0;
            while (reader.Read())
            {
                columnNameValue = (DateTimeOffset)reader[columnName];
                //_output.WriteLine(reader[count].ToString());
                count++;
                //_output.WriteLine(count.ToString());
            }
            return columnNameValue;
        }
        public DateTime ReturnSingleDateTimeValue(string sql, string columnName)
        {
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();
            reader.Read();
            return (DateTime)reader[columnName];
        }

        /**
        * Search and get a Column value
        */
        public List<DateTimeOffset> ReturnWholeDateTimeColumnValueFromSnowFlake(string sql, string columnName)
        {
            // Verify in Snowflake
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();
            List<DateTimeOffset> columnNameValue = new List<DateTimeOffset>();
            DateTimeOffset singleColumnValue = DateTimeOffset.Now;
            int count = 0;
            while (reader.Read())
            {
                singleColumnValue = (DateTimeOffset)reader[columnName];
                //_output.WriteLine(reader[count].ToString());
                columnNameValue.Add(singleColumnValue);
                count++;
                //_output.WriteLine(count.ToString());
            }
            return columnNameValue;
        }

        /**
   * Search and get a Column value
   */
        public List<string> ReturnWholeStringColumnValueFromSnowFlake(string sql, string columnName)
        {
            // Verify in Snowflake
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();
            List<string> columnNameValue = new List<string>();
            string singleColumnValue = null;
            int count = 0;
            while (reader.Read())
            {
                singleColumnValue = reader[columnName].ToString();
                //_output.WriteLine(reader[count].ToString());
                columnNameValue.Add(singleColumnValue);
                count++;
                //_output.WriteLine(count.ToString());
            }
            return columnNameValue;
        }
        /**
          * Search and get a whole table value
          */
        public string ConvertWholeTabletoJsonFormatWithUniqkey(string sql, string Uniqkey)
        {
            // Verify in Snowflake
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();

            //var r = ReadWholeRowFromSnowFlake(reader);
            var r = SerializewithUniqKey(reader, Uniqkey);

            string json = JsonConvert.SerializeObject(r, Formatting.Indented);
            //string json = JsonConvert.SerializeObject(r);

            //_output.WriteLine(json);
            return json;
        }

        /**
          * Search and get a whole table value
          */
        public string GetJsonFormatWithoutUniqkey(string sql)
        {
            // Verify in Snowflake
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();

            var r = SerializeWithoutUniqKey(reader);

            string json = JsonConvert.SerializeObject(r);

            return json;
        }

        /**
      * Search and get a whole table value
      */
        public string ConvertWholeRowtoStringFormatWithoutUniqkey(string sql, string Uniqkey)
        {
            // Verify in Snowflake
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            IDataReader reader = cmd.ExecuteReader();

            var r = ReadWholeRowFromSnowFlakeWithoutUniqKey(reader);

            string QueryResult = JsonConvert.SerializeObject(r);
            string QueryResultNew = QueryResult.Replace("\"", "");

            //_output.WriteLine(json);
            return QueryResultNew;
        }
        public Dictionary<string, object> ReadWholeRowFromSnowFlakeWithoutUniqKey(IDataReader reader)
        {
            var results = new Dictionary<string, object>();
            var cols = new List<string>();

            while (reader.Read())
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    //Console.WriteLine(string.Format("{0}:{1}", reader.GetName(i), reader[reader.GetName(i)]));
                    results.Add(reader.GetName(i), reader[reader.GetName(i)]);
                }
            }
            return results;
        }

        public Dictionary<string, object> SerializewithUniqKey(IDataReader reader, string UniqKey)
        {
            //string UniqKey = "SOMRegionId";
            var results = new Dictionary<string, object>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));
            while (reader.Read())
            {

                Dictionary<string, object> row = SerializeRow(cols, reader);
                string keyofRow = row[UniqKey].ToString();
                results.Add(UniqKey + ":" + keyofRow, row);
                //results.Add(SerializeRow(cols, reader));
            }
            return results;
        }

        public IEnumerable<Dictionary<string, object>> SerializeWithoutUniqKey(IDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));
            int m = 0;
            while (reader.Read())
            {
                m++;
                //_output.WriteLine(m.ToString());
                results.Add(SerializeRow(cols, reader));
            }
            return results;
        }

        private Dictionary<string, object> SerializeRow(IEnumerable<string> cols,
                                                        IDataReader reader)
        {
            var result = new Dictionary<string, object>();
            foreach (var col in cols)
            {
                result.Add(col, reader[col]);
                //_output.WriteLine(col+":" + reader[col].ToString());
            }
            return result;
        }

        /**
        * Return Dictionary
        */
        public IEnumerable<Dictionary<string, object>> SerializeArray(IDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));
            int m = 0;
            while (reader.Read())
            {
                m++;
                //_output.WriteLine(m.ToString());
                results.Add(SerializeRow(cols, reader));
            }
            return results;
        }

        /**
        * Return Job Status from Snowflake OMJob table
        */
        public string GetJobStatus(string jobid)
        {
            string query = "select * from \"STATIC\".\"OMJob\" where \"JobId\"='" + jobid + "' order by \"LastModifiedDate\" desc;";
            return ReturnSingleColumnValueFromSnowFlake(query, "Status");
        }

        public void DeleteRecord(string tablename, string Schema, string Id)
        {
            string deletequery = "delete from \""+Schema+"\".\""+tablename+"\" where \"UniqueIntegrationId\"='"+Id+"';";
            RunQuery(deletequery);
            string query = "select count(*) \"recs\" from \""+Schema+"\".\""+tablename+ "\" where \"UniqueIntegrationId\" ='"+Id+"';";
            Assert.Equal("0",ReturnSingleColumnValueFromSnowFlake(query, "recs"));
        }

        public string buildupSnowflakeQuery()
        {
            return null;
        }
    }
}


