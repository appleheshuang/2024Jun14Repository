
using System.Collections.Generic;
using System.Data;

namespace Sailfish.RealignmentEngine.Data
{
    /// <summary>
    /// This interface defines the functionality required to execute various operations on a tenant database.
    /// </summary>
    public interface IDataStore
    {
        /// <summary>
        /// Creates a schema in the database if it does not exist yet
        /// If exists it does not replace the old schema
        /// </summary>
        /// <param name="schemaName">Schame name</param>
        void CreateSchema(string schemaName);

        /// <summary>
        /// Creates a copy of particular schema in the database along with its data
        /// If the destination schema already exists, it replaces the old one
        /// If the source schema does not exist it creates an empty schema
        /// </summary>
        /// <param name="source">Source schame name</param>
        /// <param name="destination">Name to be given to the newly created schema</param>
        void CopySchema(string source, string destination);

        /// <summary>
        /// Swaps all objects (tables, views, etc.) and metadata, including identifiers, between the two specified schemas. 
        /// Also swaps all access control privileges granted on the schemas and objects they contain. 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        void SwapSchema(string source, string destination);

        /// <summary>
        /// Creates a copy of a source schema if the destination schema does not exist
        /// If the source schema does not exist it creates an empty schema
        /// </summary>
        /// <param name="source">Source schame name</param>
        /// <param name="destination">Name to be given to the newly created schema</param>
        void CopySchemaIfNotExists(string source, string destination);

        /// <summary>
        /// Returns the difference between a table in 2 different schemas
        /// </summary>
        /// <param name="table"></param>
        /// <param name="sourceSchema"></param>
        /// <param name="destinationSchema"></param>
        /// <returns></returns>
        DataTable GetDifference(DataTable table, string sourceSchema, string destinationSchema);

        /// <summary>
        /// Drops a particular schema in the database
        /// </summary>
        /// <param name="schemaName">Schema name to be dropped</param>
        void DropSchema(string schemaName);

        /// <summary>
        /// Get a list of scenario tables along with columns and primary keys
        /// </summary>
        /// <value>List of scenario tables</value>
        List<DataTable> ScenarioTables { get; }

        /// <summary>
        /// Get a list of Output tables along with columns and primary keys
        /// </summary>
        /// <value>List of output tables</value>
        List<DataTable> OutputTables { get; }

        /// <summary>
        /// Get a list of Output tables along with columns and primary keys
        /// </summary>
        /// <value>List of output tables</value>
        List<DataTable> StaticTables { get; }

        string InputSchema { get; }

        string OutputSchema { get; }

        string StaticSchema { get; }

        /// <summary>
        /// Scenario root table
        /// </summary>
        /// <value></value>
        DataTable ScenarioRootTable { get; }

        string ScenarioPrefix { get; }

        string KeyPrefix { get; }

        IDbAccess SqlAccess { get; }
    }
}