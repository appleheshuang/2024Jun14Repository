
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sailfish.RealignmentEngine.Data
{
    /// <summary>
    /// This class implements the functionality required to execute various operations on a tenant database.
    /// </summary>
    public class SnowflakeDatabase : IDataStore
    {
        private static readonly string ROOT_SCENARIO_TABLE = "OMScenarioScenario";

        public string[] RuleTableNames => new[]
        {
            "OMAccount",
            "OMAffiliation", 
            "OMGeography",
            "OMRule",
            "OMSalesForce",
            "OMTerritory",
            "OMTerritorySalesForce",
            "OMGeographyTerritory",
            "OMGeographyAccount",
            "OMAccountTerritoryRule",
            "OMAccountTerritory",
            "OMAccountExclusion",
            "OMAccountExplicit",
        };


        public string[] RuleTableColumns => new[]
        {
            "EffectiveDate",
            "EndDate"
        };
        
        public SnowflakeDatabase(IDbAccess dbAccess, string inputSchema = "INPUT", string outputSchema = "OUTPUT", string staticSchema="STATIC", string scenarioPrefix="Scenario", string keyPrefix="S")
        {
            SqlAccess = dbAccess;
            InputSchema = inputSchema;
            OutputSchema = outputSchema;
            StaticSchema = staticSchema;
            ScenarioPrefix = scenarioPrefix;
            KeyPrefix  = keyPrefix;
            FillTables();
        }

        /// <summary>
        /// Creates a schema in the database if it does not exist yet
        /// If exists it does not replace the old schema
        /// </summary>
        /// <param name="schemaName">Schame name</param>
        public void CreateSchema(string schemaName)
        {

            string result;
            string sql = string.Format("create schema if not exists {0};", schemaName);
            result = SqlAccess.ExecuteScalar<String>(sql);
            if (!(result.Contains("success") || result.Contains("already exists")))
                throw new RealignmentEngineException(string.Format("Failed to create schema: {0}", result));
        }

        /// <summary>
        /// Creates a copy of particular schema in the database along with its data
        /// If the destination schema already exists, it replaces the old one
        /// If the source schema does not exist it creates an empty schema
        /// </summary>
        /// <param name="source">Source schame name</param>
        /// <param name="destination">Name to be given to the newly created schema</param>
        public void CopySchema(string source, string destination)
        {
            try
            {
                string sql = string.Format("create or replace schema {0} clone {1};", destination, source);
                SqlAccess.ExecuteScalar<String>(sql);
            }
            catch (Exception e)
            {
                throw new RealignmentEngineException($"Failed to copy schema {e.Message}");
            }
        }

        /// <summary>
        /// Swaps all objects (tables, views, etc.) and metadata, including identifiers,
        /// between the two specified schemas. Also swaps all access control privileges granted on the schemas and objects they contain. 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public void SwapSchema(string source, string destination)
        {
            try
            {
                string sql = $"alter schema {destination} swap with {source}";
                SqlAccess.ExecuteScalar<String>(sql);
            }
            catch (Exception e)
            {
                throw new RealignmentEngineException($"Failed to swap schema {e.Message}");
            }
        }

        /// <summary>
        /// Creates a copy of a source schema if the destination schema does not exist
        /// If the source schema does not exist it creates an empty schema
        /// </summary>
        /// <param name="source">Source schame name</param>
        /// <param name="destination">Name to be given to the newly created schema</param>
        public void CopySchemaIfNotExists(string source, string destination)
        {
            string sql = string.Format("create schema if not exists {0} clone {1};", destination, source);
            var result = SqlAccess.ExecuteScalar<String>(sql);
            if (!result.Contains("success") && !result.Contains("already exists"))
                throw new RealignmentEngineException(string.Format("Failed to copy schema: {0}", result));
        }

        /// <summary>
        /// Drops a particular schema in the database
        /// </summary>
        /// <param name="schemaName">Schema name to be dropped</param>
        public void DropSchema(string schemaName)
        {

            string result;
            string sql = string.Format("drop schema {0}", schemaName);
            result = SqlAccess.ExecuteScalar<String>(sql);
            if (!result.Contains("success"))
                throw new RealignmentEngineException(string.Format("Failed to drop schema: {0}", result));
        }

        public IDbAccess SqlAccess { get; }

        public string ScenarioPrefix { get; }

        public string KeyPrefix { get; }

        public string InputSchema { get; }

        public string OutputSchema { get; }

        public string StaticSchema { get; }

        public List<DataTable> ScenarioTables { get; set; }

        public DataTable ScenarioRootTable { get; set; }

        public List<DataTable> OutputTables { get; set; }

        public List<DataTable> SalesforceOutputTables { get; set; }

        public List<DataTable> StaticTables { get; set; }

        public DataSet AllTables {get; set;}

        private void FillTables()
        {
            SchemaReader reader = new SchemaReader();
            var dataSet = reader.GetSnowflakeDataSet(SqlAccess, new [] { OutputSchema, StaticSchema, InputSchema });
            
            StaticTables = new List<DataTable>();
            OutputTables = new List<DataTable>();
            ScenarioTables = new List<DataTable>();

            if(ScenarioRootTable == null)
                ScenarioRootTable = dataSet.Tables[ROOT_SCENARIO_TABLE];

            foreach (DataTable table in dataSet.Tables)
            {
                if(table.Namespace == OutputSchema)
                    OutputTables.Add(table);
                if (table.Namespace == StaticSchema)
                    StaticTables.Add(table);
                if (table.Namespace == InputSchema && table.TableName != ROOT_SCENARIO_TABLE)
                {
                    //Do hack = true
                    if(table.TableName!="OMScenarioTerritory")
                        ScenarioTables.Add(table);
                }
            }
            SalesforceOutputTables = SortOutputDataTables();

            AllTables = dataSet;
        }

        /// <summary>
        /// Returns the difference between a given table in 2 different schemas
        /// </summary>
        /// <param name="table"></param>
        /// <param name="sourceSchema"></param>
        /// <param name="destinationSchema"></param>
        /// <returns></returns>
        public DataTable GetDifference(DataTable table, string sourceSchema, string destinationSchema)
        {
            string tableName = table.TableName;

            string sql =
                $@"
                SELECT * FROM ""{sourceSchema}"".""{tableName}""
                except
                SELECT * FROM ""{destinationSchema}"".""{tableName}""
                ";

            var difference = SqlAccess.ExecuteTable(sql);

            return difference;
        }

        private List<DataTable> SortOutputDataTables()
        {
            var excludedTables = new [] {"OMAccountTerritoryRule", "OMGeographyAccount"}; 
            var orderedOutputTables = new List<DataTable>(); 
            
            var sfdcSchema = new List<TableSchema>();
            var staticTableNames = StaticTables.Select(st => st.TableName).ToList();
            
            if(OutputTables != null && OutputTables.Any())
            {
                OutputTables.ForEach(t => {
                    if(!excludedTables.Contains(t.TableName))
                    {
                        var sfdcTable = new TableSchema{ TableName = t.TableName, Fks = new List<Fk>()};
                        
                        foreach(Constraint c in t.Constraints)
                        {
                            if(c is ForeignKeyConstraint fc)
                            {
                                DataColumn col = ((ForeignKeyConstraint) c).Columns[0];
                                col.ExtendedProperties.Add("IsForeignKey","True");
                                if(!staticTableNames.Contains(fc.RelatedTable.TableName))
                                {
                                    var fk = new Fk{ Name = fc.ConstraintName, ColumnName = fc.Columns.FirstOrDefault().ColumnName, RelatedTableName = fc.RelatedTable.TableName, RelatedColumnName = fc.RelatedColumns.FirstOrDefault().ColumnName };
                                    sfdcTable.Fks.Add(fk);
                                }
                            }
                        }
                        sfdcSchema.Add(sfdcTable);
                    }
                });
                
                while(orderedOutputTables.Count() != sfdcSchema.Count())
                {
                    var processedTables = orderedOutputTables.Select(t2 => t2.TableName);
                    sfdcSchema.Where(t => !processedTables.Contains(t.TableName) && t.Fks.All(fk => processedTables.Contains(fk.RelatedTableName)))
                    .ToList().ForEach(subTable => orderedOutputTables.Add(OutputTables.FirstOrDefault(ot => ot.TableName == subTable.TableName)));
                }
            }
            
            return orderedOutputTables;
        }
    }

    internal class TableSchema
    {
        public string TableName { get; set; } 

        public List<Fk> Fks { get; set; } 
    }

    internal class Fk{
        public string Name { get; set; }
        public string ColumnName { get; set; }
        public string RelatedColumnName { get; set; }
        public string RelatedTableName { get; set; }
    }
}