using Azure;
using Azure.Data.Tables;

namespace Image_Reconstruction_Classifier
{
    /// <summary>
    /// Azure Table Storage entity holding one image-reconstruction similarity comparison result,
    /// as saved by <see cref="ResultStorageService.SaveResultAsync"/>.
    /// </summary>
    public class ReconstructionResultEntity : ITableEntity
    {
        // Required by ITableEntity
        /// <summary>Object type (0-9) of the compared image; used to partition results by type.</summary>
        public string PartitionKey { get; set; } = string.Empty;
        /// <summary>Unique row identifier: '{imageName}_{method}_{timestamp}'.</summary>
        public string RowKey { get; set; } = string.Empty;
        /// <summary>Timestamp assigned by Azure Table Storage when the entity is written.</summary>
        public DateTimeOffset? Timestamp { get; set; }
        /// <summary>Concurrency tag assigned by Azure Table Storage.</summary>
        public ETag ETag { get; set; }

        // Our custom fields
        /// <summary>Label or name of the compared image, e.g. '3_001'.</summary>
        public string ImageName { get; set; } = string.Empty;
        /// <summary>Object type 0-9 of the compared image.</summary>
        public string ObjectType { get; set; } = string.Empty;
        /// <summary>Reconstruction method used: 'HTM', 'KNN', or 'Combined'.</summary>
        public string Method { get; set; } = string.Empty;
        /// <summary>Cosine similarity between original and reconstructed image.</summary>
        public double CosineSimilarity { get; set; }
        /// <summary>Percentage of matching pixels between original and reconstructed image.</summary>
        public double BinarySimilarity { get; set; }
        /// <summary>Name of the reconstructed image blob in Azure Blob Storage.</summary>
        public string ReconstructedBlobName { get; set; } = string.Empty;
        /// <summary>UTC timestamp (yyyy-MM-dd HH:mm:ss) recorded when this result was saved.</summary>
        public string DeploymentDate { get; set; } = string.Empty;
    }
}
