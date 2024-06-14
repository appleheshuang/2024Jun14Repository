using System.Data;

namespace Sailfish.RealignmentEngine.Data
{
    /// <summary>
    /// Transactional SQL interface to a database.
    /// All Execute functions take a SQL statement and optional list of positional parameters.
    /// The database state (Open or Closed) is preserved after each Execute call - e.g. if the database connection
    /// was closed it will be temporarily opened to execute the command.
    /// </summary>
    public interface IDbAccess
    {
        /// <summary>
        /// Executes a DML statement (insert, update or delete) with optional paramaters
        /// Note: The database state will be preserved (if closed it be opened for the duration of the statement)
        /// </summary>
        /// <param name="sql">Sql query to be executed</param>
        /// <param name="parameters"></param>
        /// <returns>Number of rows affected</returns>
        int ExecuteNonQuery(string sql, params object[] parameters);

        /// <summary>
        /// Executes a select query with optional positional parameters and returns the results as a data table
        /// Note: The database state will be preserved (if closed it be opened for the duration of the statement)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns>DateTable containing rows of query</returns>
        DataTable ExecuteTable(string sql, params object[] parameters);


        /// <summary>
        /// Executes a select query with optional positional parameters and returns the results as a data table
        /// Note: The database state will be preserved (if closed it be opened for the duration of the statement)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns>DateTable containing rows of query</returns>
        IDataReader ExecuteReader(string sql, params object[] parameters);

        /// <summary>
        /// Executes a select query with optional positional parameters and returns only the first row and column, converting it to the requested type T
        /// Note: The database state will be preserved (if closed it be opened for the duration of the statement)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns>DateTable containing rows of query</returns>
        T ExecuteScalar<T>(string sql, params object[] parameters);

        /// <summary>
        /// Executes a select query with optional positional parameters and returns only the first row and column
        /// Note: The database state will be preserved (if closed it be opened for the duration of the statement)
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns>DateTable containing rows of query</returns>
        object ExecuteScalar(string sql, params object[] parameters);

        /// <summary>
        /// Begin a database transaction
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Determines whether the current database session is in a transaction
        /// </summary>
        /// <value></value>
        bool InTransaction { get; }

        /// <summary>
        /// Commit the current database transaction
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollback the current database transaction
        /// </summary>
        void Rollback();
    }
}