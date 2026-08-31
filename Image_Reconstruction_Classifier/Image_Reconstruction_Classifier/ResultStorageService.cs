using Azure.Data.Tables;

namespace Image_Reconstruction_Classifier
{
    /// <summary>
    /// Persists and queries image-reconstruction comparison results in the "ReconstructionResults"
    /// Azure Table Storage table.
    /// </summary>
    public class ResultStorageService
    {
        private readonly TableClient _tableClient;
        private const string TableName = "ReconstructionResults";

        /// <summary>
        /// Creates the service and ensures the "ReconstructionResults" table exists.
        /// </summary>
        /// <param name="connectionString">Azure Storage account connection string.</param>
        public ResultStorageService(string connectionString)
        {
            _tableClient = new TableClient(connectionString, TableName);
            _tableClient.CreateIfNotExists();
            CloudLogger.LogInfo("ResultStorageService", $"Connected to table '{TableName}'");
        }

        // ============================================================
        // Save a single reconstruction result
        // ============================================================
        /// <summary>
        /// Saves one comparison result as a table entity, partitioned by object type with a
        /// row key unique per image, method, and timestamp.
        /// </summary>
        /// <param name="imageName">Label or name of the compared image, e.g. '3_001'.</param>
        /// <param name="objectType">Object type 0-9; used as the partition key.</param>
        /// <param name="method">Reconstruction method used: 'HTM', 'KNN', or 'Combined'.</param>
        /// <param name="cosineSimilarity">Cosine similarity between original and reconstructed image.</param>
        /// <param name="binarySimilarity">Percentage of matching pixels between original and reconstructed image.</param>
        /// <param name="reconstructedBlobName">Name of the reconstructed blob in Azure.</param>
        public async Task SaveResultAsync(
            string imageName,
            string objectType,
            string method,
            double cosineSimilarity,
            double binarySimilarity,
            string reconstructedBlobName)
        {
            try
            {
                var entity = new ReconstructionResultEntity
                {
                    // PartitionKey = object type (0-9)
                    PartitionKey = objectType,
                    // RowKey = unique per image + method
                    RowKey = $"{imageName}_{method}_{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    ImageName = imageName,
                    ObjectType = objectType,
                    Method = method,
                    CosineSimilarity = cosineSimilarity,
                    BinarySimilarity = binarySimilarity,
                    ReconstructedBlobName = reconstructedBlobName,
                    DeploymentDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                await _tableClient.AddEntityAsync(entity);
                CloudLogger.LogInfo("ResultStorageService",
                    $"Saved result: {imageName} | {method} | Cosine={cosineSimilarity:F4} | Binary={binarySimilarity:F2}%");
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ResultStorageService", $"Error saving result for {imageName}", ex);
                throw;
            }
        }

        // ============================================================
        // Get all results for a specific object type
        // ============================================================
        /// <summary>
        /// Retrieves all saved results for a given object type.
        /// </summary>
        /// <param name="objectType">Object type 0-9 to filter by (matches the partition key).</param>
        /// <returns>The matching result entities.</returns>
        public async Task<List<ReconstructionResultEntity>> GetResultsByObjectTypeAsync(string objectType)
        {
            try
            {
                var results = new List<ReconstructionResultEntity>();

                await foreach (var entity in _tableClient.QueryAsync<ReconstructionResultEntity>(
                    filter: TableClient.CreateQueryFilter($"PartitionKey eq {objectType}")))
                {
                    results.Add(entity);
                }

                CloudLogger.LogInfo("ResultStorageService",
                    $"Retrieved {results.Count} results for object type '{objectType}'");
                return results;
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ResultStorageService",
                    $"Error retrieving results for type {objectType}", ex);
                throw;
            }
        }

        // ============================================================
        // Get all results
        // ============================================================
        /// <summary>
        /// Retrieves every saved result across all object types.
        /// </summary>
        /// <returns>All result entities in the table.</returns>
        public async Task<List<ReconstructionResultEntity>> GetAllResultsAsync()
        {
            try
            {
                var results = new List<ReconstructionResultEntity>();

                await foreach (var entity in _tableClient.QueryAsync<ReconstructionResultEntity>())
                {
                    results.Add(entity);
                }

                CloudLogger.LogInfo("ResultStorageService", $"Retrieved {results.Count} total results");
                return results;
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ResultStorageService", "Error retrieving all results", ex);
                throw;
            }
        }
    }
}
