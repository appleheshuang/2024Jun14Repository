using MainFramework.GlobalUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace OMREngineTest.Utils
{
    class SnowflakeFunction
    {
        /*public void deleteSchema(string strSchema, string connStr)
        {            
            var sqlQuery = "select TABLE_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLES WHERE ROW_COUNT >= 1 AND TABLE_NAME NOT IN ('SchemaKey') AND TABLE_SCHEMA NOT LIKE 'SC_%' AND TABLE_SCHEMA = '"+ strSchema + "'";

            //SnowFlakeHelper _snowflake = new SnowFlakeHelper();

            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connStr;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;
            IDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var tblName = reader["TABLE_NAME"].ToString();
                var schemaName = reader["TABLE_SCHEMA"].ToString();
                var deleteStmt = "DELETE FROM \""+ schemaName  +"\".\""+ tblName  +"\";";
                cmd.CommandText = deleteStmt;
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = "COMMIT;";
            cmd.ExecuteNonQuery();

            conn.Close();
        }*/

        public void dropExcessSchema(string connStr, string pattern)
        {
            var sqlQuery = "select SCHEMA_NAME from INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME LIKE '%"+pattern+"%';";

            //SnowFlakeHelper _snowflake = new SnowFlakeHelper();

            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connStr;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;
            IDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var schemaName = reader["SCHEMA_NAME"].ToString();
                var dropStmt = "DROP SCHEMA "+ schemaName + ";";
                cmd.CommandText = dropStmt;
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = "COMMIT;";
            cmd.ExecuteNonQuery();

            conn.Close();
        }

        /*public void deleteTable(string connStr, string tableName)
        {
            var sqlQuery = "select TABLE_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('"+tableName+"')";

            //SnowFlakeHelper _snowflake = new SnowFlakeHelper();

            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connStr;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;
            IDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var tblName = reader["TABLE_NAME"].ToString();
                var schemaName = reader["TABLE_SCHEMA"].ToString();
                var deleteStmt = "DELETE FROM \"" + schemaName + "\".\"" + tblName + "\";";
                cmd.CommandText = deleteStmt;
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = "COMMIT;";
            cmd.ExecuteNonQuery();

            conn.Close();
        }*/

        /*public void updateCreatedModifiedDateAfterBulkLoad(string connStr)
        {
            var sqlQuery = "SELECT distinct tbl.TABLE_SCHEMA, tbl.TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS col " +
                "JOIN INFORMATION_SCHEMA.TABLES tbl" +
                "  ON col.TABLE_NAME = tbl.TABLE_NAME" +
                "  AND tbl.ROW_COUNT >= 1" +
                "  AND tbl.TABLE_NAME not in ('SchemaKey', 'BulkLoadStatus', 'BulkLoadError')" +
                "WHERE 1 = 1" +
                "AND col.COLUMN_NAME in (" +
                "'LastModifiedDate'" +
                ", 'CreatedDate'" +
                ") order by tbl.TABLE_NAME asc";

            //SnowFlakeHelper _snowflake = new SnowFlakeHelper();

            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connStr;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;
            IDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var tblName = reader["TABLE_NAME"].ToString();
                var schemaName = reader["TABLE_SCHEMA"].ToString();
                var updateCreateDate = "UPDATE \""+ schemaName + "\".\""+ tblName + "\" SET \"CreatedDate\" = current_timestamp();";                
                cmd.CommandText = updateCreateDate;
                cmd.ExecuteNonQuery();
                var updateLastModifiedDate = "UPDATE \""+ schemaName + "\".\"" + tblName + "\" SET \"LastModifiedDate\" = current_timestamp();";
                cmd.CommandText = updateLastModifiedDate;
                cmd.ExecuteNonQuery();
                cmd.CommandText = "COMMIT;";
                cmd.ExecuteNonQuery();

            }
            conn.Close();
        }*/

        public void deleteFromTablesWRecord(string connStr, List<string> tablelist = null)
        {
            var sqlQuery = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES where ROW_COUNT > 0 and TABLE_SCHEMA in ('OUTPUT','STATIC')";
            sqlQuery += tablelist != null && tablelist.Count > 0 ? " and TABLE_NAME in ('"+string.Join("','",tablelist)+"');" : ";";

            //SnowFlakeHelper _snowflake = new SnowFlakeHelper();

            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connStr;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;
            IDataReader reader = cmd.ExecuteReader();

            while (reader.Read())

            {
                var tblName = reader["TABLE_NAME"].ToString();
                var schemaName = reader["TABLE_SCHEMA"].ToString();
                var deleteStmt = "DELETE FROM \"" + schemaName + "\".\"" + tblName + "\";";
                cmd.CommandText = deleteStmt;
                cmd.ExecuteNonQuery();
                cmd.CommandText = "COMMIT;";
                cmd.ExecuteNonQuery();

            }
            conn.Close();
        }
    }
}
