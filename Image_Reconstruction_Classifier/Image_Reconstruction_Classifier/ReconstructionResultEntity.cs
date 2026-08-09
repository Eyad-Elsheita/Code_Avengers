using Azure;
using Azure.Data.Tables;

namespace Image_Reconstruction_Classifier
{
    public class ReconstructionResultEntity : ITableEntity
    {
        // Required by ITableEntity
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Our custom fields
        public string ImageName { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public double CosineSimilarity { get; set; }
        public double BinarySimilarity { get; set; }
        public string ReconstructedBlobName { get; set; } = string.Empty;
        public string DeploymentDate { get; set; } = string.Empty;
    }
}
