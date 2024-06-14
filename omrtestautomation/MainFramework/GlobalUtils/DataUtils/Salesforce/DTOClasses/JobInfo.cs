using System;
using System.Collections.Generic;

namespace RealignmentEngine.Salesforce.DTOClasses
{
    public class JobInfo
    {
        public string Id { get; set; }
        public string Operation { get; set; }
        public string Object { get; set; }
        public DateTime CreatedDate { get; set; }
        public string State { get; set; }
        public string ConcurrencyMode { get; set; }
        public string ContentType { get; set; }
        public string ApiVersion { get; set; }
        public string ColumnDelimiter { get; set; }
        public string JobType { get; set; }
        public int NumberRecordsProcessed { get; set; }
        public int Retries { get; set; }
        public int TotalProcessingTime { get; set; }

        public bool Completed => !string.IsNullOrWhiteSpace(this.State) && "Aborted_JobComplete_Failed".Contains(this.State);
    }
}