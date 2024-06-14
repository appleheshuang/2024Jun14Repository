using System;
using System.Data;

namespace Sailfish.RealignmentEngine.Data
{
    /// <summary>
    /// Generic database access using System.Data
    /// 
    /// Usage:
    ///     var db = new DbAccess(snowflakeConnection);
    ///     db.ExecuteNonQuery("sql");
    ///     ...
    /// 
    ///     db.BeginTransaction();
    ///     db.ExecuteNonQuery("sql");
    ///     ...
    ///     db.Commit();
    /// </summary>
    public class DbAccess : IDbAccess
    {
        public DbAccess(IDbConnection dbConnection)
        {
            DbConnection = dbConnection;
        }

        /// <summary>
        /// This event is fired just before executing a SQL statement - useful for trace logging
        /// </summary>
        public event EventHandler<DbEventArgs> OnExecuting;

        /// <summary>
        /// Executes a DML statement (insert, update, delete) with optional parameters (denoted by ? in sql) and returns results as data table
        /// Note: Snowflake.Data does not support named parameters
        /// </summary>
        /// <param name="sql">select sql statement</param>
        /// <param name="parameters">positional paramaters identified by ? in the query</param>
        /// <returns>Number of rows affected by sql statement</returns>
        public int ExecuteNonQuery(string sql, params object[] parameters) { return UsingConnection(() => ExecuteNonQueryInternal(sql, parameters)); }

        /// <summary>
        /// Executes a select query with optional parameters (denoted by ? in sql) and returns results as data table
        /// Note: Snowflake.Data does not support named parameters
        /// </summary>
        /// <param name="sql">select sql statement</param>
        /// <param name="parameters">positional paramaters identified by ? in the query</param>
        /// <returns>Unnamed DataTable containing all rows fetched from sql query</returns>
        public DataTable ExecuteTable(string sql, params object[] parameters) { return UsingConnection(() => ExecuteTableInternal(sql, parameters)); }

        /// <summary>
        /// Executes a select query with optional parameters (denoted by ? in sql) and returns results as data reader
        /// Note: Snowflake.Data does not support named parameters
        /// </summary>
        /// <param name="sql">select sql statement</param>
        /// <param name="parameters">positional paramaters identified by ? in the query</param>
        /// <returns>Unnamed DataTable containing all rows fetched from sql query</returns>
        public IDataReader ExecuteReader(string sql, params object[] parameters) { return UsingConnection(() => ExecuteReaderInternal(sql, parameters)); }

        /// <summary>
        /// Executes a select query with optional parameters (denoted by ? in sql) and returns the first row and column converted to the required type T.
        /// Will throw an exception if the value returned by the query can't be converted to T. This can happen if no data is returned from the query.
        /// Note: Snowflake.Data does not support named parameters
        /// </summary>
        /// <param name="sql">select sql statement</param>
        /// <param name="parameters">positional paramaters identified by ? in the query</param>
        /// <returns>First row and column returned from query as T</returns>
        public T ExecuteScalar<T>(string sql, params object[] parameters) { return UsingConnection(() => ExecuteScalarInternal<T>(sql, parameters)); }

        /// <summary>
        /// Executes a select query with optional parameters (denoted by ? in sql) and returns the first row and column as raw data
        /// Note: Snowflake.Data does not support named parameters
        /// </summary>
        /// <param name="sql">select sql statement</param>
        /// <param name="parameters">positional paramaters identified by ? in the query</param>
        /// <returns>First row and column returned from query or null if no data was fetched</returns>
        public object ExecuteScalar(string sql, params object[] parameters) { return UsingConnection(() => ExecuteScalarInternal(sql, parameters)); }

        /// <summary>
        /// Starts a database transaction on this connection
        /// </summary>
        public void BeginTransaction()
        {
            autoClose = DbConnection.State == ConnectionState.Closed;
            if (autoClose)
                DbConnection.Open();

            DbTransaction = DbConnection.BeginTransaction();
        }

        /// <summary>
        /// Determines whether this connection is running in the context of a database transaction
        /// </summary>
        /// <value></value>
        public bool InTransaction { get { return DbTransaction != null; } }

        /// <summary>
        /// Commits (and closes) the current database transaction. Behaviour reverts to auto commit until another BeginTransaction is called.
        /// </summary>
        public void Commit()
        {
            try { DbTransaction.Commit(); }
            finally
            {
                DbTransaction = null;
                if (autoClose)
                    DbConnection.Close();
            }
        }

        /// <summary>
        /// Rolls back (and closes) the current database transaction. Behaviour reverts to auto commit until another BeginTransaction is called.
        /// </summary>
        public void Rollback()
        {
            try { DbTransaction.Rollback(); }
            finally
            {
                DbTransaction = null;
                if (autoClose)
                    DbConnection.Close();
            }
        }

        protected int ExecuteNonQueryInternal(string sql, params object[] parameters)
        {
            return UsingCommand((cmd) => cmd.ExecuteNonQuery(), sql, parameters);
        }

        public DataTable ExecuteTableInternal(string sql, params object[] parameters)
        {
            return UsingCommand((cmd) =>
            {
                var table = new DataTable();
                using (var reader = cmd.ExecuteReader())
                {
                    // get columns
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var type = reader.GetFieldType(i);
                        table.Columns.Add(name, type);
                    }

                    // read all rows
                    var values = new object[reader.FieldCount];
                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        table.Rows.Add(values);
                    }
                    return table;
                }
            }, sql, parameters);
        }

        IDataReader ExecuteReaderInternal(string sql, params object[] parameters)
        {
            return UsingCommand((cmd) => cmd.ExecuteReader(), sql, parameters);
        }

        T ExecuteScalarInternal<T>(string sql, params object[] parameters)
        {
            var r = ExecuteScalarInternal(sql, parameters);
            return (T)Convert.ChangeType(r, typeof(T));
        }

        object ExecuteScalarInternal(string sql, params object[] parameters)
        {
            return UsingCommand((cmd) =>
            {
                //cmd.ExecuteScalar() => Snowflake returns DBNull for no rows
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null; // no data returned
                    return reader.GetValue(0);
                }
            }, sql, parameters);
        }

        /// <summary>
        /// Wraps a database operation within using{Open; func; Close} if the connection is closed, otherwise func is called directly
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T UsingConnection<T>(Func<T> func)
        {
            if (DbConnection.State == ConnectionState.Open) return func();

            using (DbConnection)
            {
                DbConnection.Open();
                var r = func();
                DbConnection.Close();
                return r;
            }
        }

        /// <summary>
        /// Execute internal command actions on a newly create database command from sql with optional parameters
        /// Handles logging and includes failing sql on exceptions
        /// </summary>
        /// <param name="command"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T UsingCommand<T>(Func<IDbCommand, T> command, string sql, object[] parameters)
        {
            using (var cmd = DbConnection.CreateCommand())
            {
                cmd.Transaction = DbTransaction;
                cmd.CommandText = sql;
                if (OnExecuting != null) OnExecuting(this, new DbEventArgs { Sql = sql, Parameters = parameters });

                // create parameters
                for (int i = 0; i < parameters.Length; i++)
                {
                    var p1 = cmd.CreateParameter();
                    p1.ParameterName = $"{i + 1}";
                    p1.Value = parameters[i];
                    p1.DbType = DbTypeFor(parameters[i]);
                    cmd.Parameters.Add(p1);
                }

                try
                {
                    return command(cmd);
                }
                catch (Exception ex)
                {
                    ex.Data["sql"] = sql;
                    ex.Data["parameters"] = parameters;
                    throw;
                }
            }
        }


        /// <summary>
        /// Determine DbType from an object's data type
        /// Note: can't do this with polymorphism as value types have already been boxed
        /// </summary>
        /// <param name="x">Object to determine DbType of</param>
        /// <returns>DbType of object OR DbType.String for unrecognized types</returns>
        static DbType DbTypeFor(object x)
        {
            if (x is string) return DbType.String;
            if (x is DateTime) return DbType.DateTime;
            if (x is Int32 || x is Int64) return DbType.Int64;
            if (x is byte[]) return DbType.Binary;
            return DbType.String;
        }


        public IDbConnection DbConnection { get; protected set; }
        public IDbTransaction DbTransaction { get; protected set; }
        private bool autoClose;
    }

    public class DbEventArgs : EventArgs
    {
        public string Sql;
        public object[] Parameters;
        public int Result;
        public TimeSpan ProcTime;
    }
}