using Azure;
using Azure.Data.Tables;
using MyCloudProject.Common;
using System;
using System.Runtime.Serialization;

namespace MyExperiment
{
    /// <summary>
    /// The experiment result record written to Azure Table Storage.
    ///
    /// IMPORTANT: Azure Table Storage only supports a limited set of property types
    /// (byte[], bool, DateTime, double, Guid, int, long, string). TimeSpan and arrays
    /// are NOT supported and cause the write to fail or silently drop the value.
    /// So the convenience-typed members (Duration as TimeSpan, OutputFiles as string[])
    /// are marked [IgnoreDataMember] and are backed by table-safe fields
    /// (DurationSec as long, OutputFilesCsv as string).
    /// </summary>
    public class ExperimentResult : ITableEntity, IExperimentResult
    {
        public ExperimentResult(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        // --- ITableEntity required members ---
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // --- Experiment metadata (all table-safe types) ---
        public string ExperimentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public string InputFileUrl { get; set; }

        // Duration stored as seconds (long is table-safe). This is the persisted value.
        public long DurationSec { get; set; }

        // Headline accuracy (double is table-safe; float widens to double fine).
        public float Accuracy { get; set; }

        // Output files persisted as a single semicolon-joined string (string is table-safe).
        public string OutputFilesCsv { get; set; }

        // --- Convenience-typed members NOT persisted directly to the table ---

        /// <summary>
        /// TimeSpan is not a supported Azure Table type. Ignored on write; backed by DurationSec.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan Duration
        {
            get => TimeSpan.FromSeconds(DurationSec);
            set => DurationSec = (long)value.TotalSeconds;
        }

        /// <summary>
        /// string[] is not a supported Azure Table type. Ignored on write; backed by OutputFilesCsv.
        /// </summary>
        [IgnoreDataMember]
        public string[] OutputFiles
        {
            get => string.IsNullOrEmpty(OutputFilesCsv)
                ? Array.Empty<string>()
                : OutputFilesCsv.Split(';', StringSplitOptions.RemoveEmptyEntries);
            set => OutputFilesCsv = value == null ? "" : string.Join(";", value);
        }
    }
}