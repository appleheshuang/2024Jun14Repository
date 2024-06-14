
using Sailfish.RealignmentEngine.Data;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sailfish.RealignmentEngine.Scenario
{
    public class SnowflakeDataSource : ISnowflakeDataSource
    {
        private readonly IDataStore _dataStore;
        private readonly IExternalStorage _storage;

        private List<string> _salesforceStandards = new List<string>() { "Name", "Id", "CreatedById", "LastModifiedById", "OwnerId", "CreatedDate", "LastModifiedDate" };
        private List<string> _exclusionList = new List<string>(){"SourceId", "CreatedDate", "LastModifiedDate"};

        private ILogger _log;

        public SnowflakeDataSource(IDataStore dataStore, IExternalStorage externalStorage, ILogger log)
        {
            _dataStore = dataStore;
            _storage = externalStorage;
            _log = log;
        }

        /// <summary>
        /// Push scenario data from external storage to a schema in Snowflake
        /// </summary>
        /// <param name="key">location of scenario data in the external storage</param>
        /// <param name="scenarioSchema">Schema name where scenario data is pushed to</param>
        public void PushScenarioData(string key, string scenarioSchema)
        {
            var scenarioRootTable = _dataStore.ScenarioRootTable;
            if (scenarioRootTable != null)
            {
                string columns = String.Join(",", scenarioRootTable.Columns.Cast<DataColumn>().Select(x =>
                    $"\"{x.ColumnName}\"").ToList());
                DownloadDataForTable(key, scenarioSchema, scenarioRootTable.TableName, columns);
            }

            var tables = _dataStore.ScenarioTables;
            foreach (var table in tables)
            {
                string columns = String.Join(",", table.Columns.Cast<DataColumn>().Select(x => $"\"{x.ColumnName}\"").ToList());
                DownloadDataForTable(key, scenarioSchema, table.TableName, columns);
            }
        }

        /// <summary>
        /// Pull output data from Snowflake to external storage
        /// </summary>
        /// <param name="scenarioId">Scenario ID</param>
        /// <param name="outputSchema">Output schema name</param>
        /// <returns>location of output data in the external storage</returns>
        /// 
        public string PullOutputData(string scenarioId, string outputSchema)
        {
            string key = string.Format("Output/{0}/{1}", scenarioId,  Guid.NewGuid().ToString());
            var tables = _dataStore.OutputTables;
            foreach (var table in tables)
            {
                var cols = table.Columns.Cast<DataColumn>();
                var columns = cols as DataColumn[] ?? cols.ToArray();
                string columnsAlias = string.Join(", ", columns.Where(col => Exclude(col.ColumnName)).Select(col => MapToSalesforce(col, _dataStore.KeyPrefix)));

                string diffSql =
                    $@"
                    CREATE OR REPLACE TABLE ""{outputSchema}"".""{table.TableName}_stage"" as
                    SELECT {columnsAlias} FROM ""{_dataStore.OutputSchema}"".""{table.TableName}""
                    except
                    SELECT {columnsAlias} FROM ""{outputSchema}"".""{table.TableName}""
                    ";

                _dataStore.SqlAccess.ExecuteNonQuery(diffSql);

                string s3Folder = $"{_storage.Host}/{key}/{table.TableName}/";

                string uploadSql = 
                    $@"
                    COPY INTO '{s3Folder}' 
                    FROM ""{outputSchema}"".""{table.TableName}_stage""  
                    FILE_FORMAT = (COMPRESSION=NONE TYPE=CSV FIELD_OPTIONALLY_ENCLOSED_BY = '""' NULL_IF='')
                    OVERWRITE = TRUE
                    CREDENTIALS=({_storage.Credentials})
                    HEADER = TRUE;
                    ";

                _dataStore.SqlAccess.ExecuteNonQuery(uploadSql);

                string delSql = $@"DROP TABLE ""{outputSchema}"".""{table.TableName}_stage""";

                _dataStore.SqlAccess.ExecuteNonQuery(delSql);

                _log.LogInformation($"Table {table.TableName} uploaded to {s3Folder}");

            }
            return key;
        }

        private string MapToSalesforce(DataColumn col, string keyPrefix)
        {

            if (_salesforceStandards.Contains(col.ColumnName))
            {
                return $"\"{col.ColumnName}\" AS \"{col.ColumnName}\"";
            }
            else if (col.ColumnName.StartsWith(keyPrefix)&&((col.ExtendedProperties["IsForeignKey"] ?? string.Empty).ToString()=="True"))
            {
                string lookupColumn = col.ColumnName.Substring(col.ColumnName.IndexOf(keyPrefix)+1);
                return $"\"{col.ColumnName}\" AS \"{lookupColumn}__r.{col.ColumnName}__c\"";
            }
            else
            {
                return $"\"{col.ColumnName}\" AS \"{col.ColumnName}__c\"";
            }
        }

        private void DownloadDataForTable(string key, string scenarioSchema, string tableName, string columns)
        {
            string dataFolder = $"{_storage.Host}/{key}/{tableName}/";
            string snowflakeStagingTable = $"\"{scenarioSchema}\".\"{tableName}\"";
            try
            {
                int rowsAffected;
                _log.LogDebug($"Downloading into table {snowflakeStagingTable}...");
                string sql = $"COPY INTO {snowflakeStagingTable} ({columns}) FROM '{dataFolder}' credentials=({_storage.Credentials}) FILE_FORMAT=(NULL_IF=\"\" TRIM_SPACE=true TYPE = CSV SKIP_HEADER = 1 FIELD_OPTIONALLY_ENCLOSED_BY = '\"')";
                rowsAffected = _dataStore.SqlAccess.ExecuteNonQuery(sql);
                _log.LogInformation($"{rowsAffected} row/s downloaded");
            }
            catch (Exception ex)
            {
                throw new RealignmentEngineException($"Could not load {dataFolder} into {snowflakeStagingTable}", ex);
            }
        }

        private bool Exclude(string columnName)
        {
            if (_exclusionList.Contains(columnName))
                return false;
            return true;
        }
    }
}