
using System;

namespace Sailfish.RealignmentEngine
{
    public abstract class EngineException : AggregateException
    {
        public EngineException() :base()
        {
        }
        public EngineException(string message)
            : base(message)
        {
            ErrorMessage = message;
        }

        public EngineException(string message, Exception inner)
            : base(message, inner)
        {
            ErrorMessage = message;
        }

        public EngineException(string tableName, string primaryKey, string errorCode, string message)
            : this(message)
        {
            TableName = tableName;
            PrimaryKey = primaryKey;
            ErrorCode = errorCode;
        }

        public EngineException(string tableName, string primaryKey, string errorCode, string message, Exception inner)
            : this(message, inner)
        {
            TableName = tableName;
            PrimaryKey = primaryKey;
            ErrorCode = errorCode;
        }
          public string TableName { get; set; }   
          public string PrimaryKey { get; set; } 
          public string ErrorCode { get; set; } 
          public string ErrorMessage { get; set; }   
    }

    public class RealignmentEngineException : EngineException
    {
        public RealignmentEngineException() : base()
        {
        }

        public RealignmentEngineException(string message)
            : base(message)
        {
        }

        public RealignmentEngineException(string message, Exception inner)
            : base(message, inner)
        {
        }
        public RealignmentEngineException(string tableName, string primaryKey, string errorCode, string message)
            : base(tableName, primaryKey, errorCode, message)
        {
        }
        public RealignmentEngineException(string tableName, string primaryKey, string errorCode, string message, Exception inner)
            : base(tableName, primaryKey, errorCode, message, inner)
        {
        }
    }

    public class DataMergeException : EngineException
    {
        public DataMergeException()
        {
        }

        public DataMergeException(string message)
            : base(message)
        {
        }
        public DataMergeException(string message, Exception inner)
            : base(message, inner)
        {
        }
        public DataMergeException(string tableName, string primaryKey, string errorCode, string message)
            : base(tableName, primaryKey, errorCode, message)
        {
        }
        public DataMergeException(string tableName, string primaryKey, string errorCode, string message, Exception inner)
            : base(tableName, primaryKey, errorCode, message, inner)
        {
        }
    }

    public class ScenarioSyncException : EngineException
    {
        public ScenarioSyncException()
        {
        }

        public ScenarioSyncException(string message)
            : base(message)
        {
        }
        public ScenarioSyncException(string message, Exception inner)
            : base(message, inner)
        {
        }
        public ScenarioSyncException(string tableName, string primaryKey, string errorCode, string message)
            : base(tableName, primaryKey, errorCode, message)
        {
        }
        public ScenarioSyncException(string tableName, string primaryKey, string errorCode, string message, Exception inner)
            : base(tableName, primaryKey, errorCode, message, inner)
        {
        }
    }

}