using Azure.Data.Tables;

namespace Image_Reconstruction_Classifier
{
    public class ResultStorageService
    {
        private readonly TableClient _tableClient;
        private const string TableName = "ReconstructionResults";

        public ResultStorageService(string connectionString)
        {
            _tableClient = new TableClient(connectionString, TableName);
            _tableClient.CreateIfNotExists();
            CloudLogger.LogInfo("ResultStorageService", $"Connected to table '{TableName}'");
        }

        // ============================================================
        // Save a single reconstruction result
        // ============================================================
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
