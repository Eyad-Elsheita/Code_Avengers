using System.ComponentModel;
using System.Text.Json;
using Azure.Storage.Blobs;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class KnnClassifierTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";

        // In-memory classifier — persisted to blob between sessions
        private static KnnClassifier _activeClassifier = new();
        private static string? _activeModelName;

        public KnnClassifierTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        // ============================================================
        // METHOD 1: Train KNN Classifier
        // ============================================================
        [McpServerTool, Description("Train the KNN classifier with a single SDR and its label. Call repeatedly for each training image.")]
        public Task<string> TrainKnnClassifier(
            [Description("Sparse Distributed Representation — array of active column indices")] int[] sdr,
            [Description("Class label for this SDR, e.g. digit 0-9")] int label)
        {
            try
            {
                if (sdr == null)
                    throw new ArgumentNullException(nameof(sdr), "SDR cannot be null.");

                _activeClassifier.Train(sdr, label);

                CloudLogger.LogInfo("KnnClassifierTool", $"Trained KNN with label {label}");
                return Task.FromResult($"✅ Trained KNN example for label {label}");
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("KnnClassifierTool", "Error training KNN classifier", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Classify / Predict with KNN
        // ============================================================
        [McpServerTool, Description("Classify a test SDR using the KNN classifier. Returns the predicted class label.")]
        public Task<int> PredictWithKnn(
            [Description("Test SDR — array of active column indices to classify")] int[] testSdr,
            [Description("Number of nearest neighbors to consider (typical: 3-15)")] int k = 5)
        {
            try
            {
                if (testSdr == null)
                    throw new ArgumentNullException(nameof(testSdr), "Test SDR cannot be null.");

                int predictedLabel = _activeClassifier.Classify(testSdr, k);

                CloudLogger.LogInfo("KnnClassifierTool", $"KNN predicted label: {predictedLabel} (k={k})");
                return Task.FromResult(predictedLabel);
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("KnnClassifierTool", "Error predicting with KNN", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 3: Save Model to Azure Blob
        // ============================================================
        [McpServerTool, Description("Persist the current in-memory KNN training data to Azure Blob Storage as JSON.")]
        public async Task<string> SaveModelToBlob(
            [Description("Name to save the model under, e.g. 'knn-model-digit3'")] string modelName,
            [Description("Container name: 'train' or 'test'")] string containerName = "train")
        {
            try
            {
                var dto = _activeClassifier.ExportTrainingData();
                string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = false });

                var container = _blobServiceClient.GetBlobContainerClient(containerName);
                string blobName = $"{modelName}.knn.json";
                var blobClient = container.GetBlobClient(blobName);

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(stream, overwrite: true);

                _activeModelName = modelName;

                CloudLogger.LogInfo("KnnClassifierTool", $"Saved KNN model '{modelName}' ({dto.Count} examples) to {containerName}/{blobName}");
                return $"✅ Saved {dto.Count} KNN training examples to {containerName}/{blobName}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("KnnClassifierTool", "Error saving KNN model to blob", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 4: Load Model from Azure Blob
        // ============================================================
        [McpServerTool, Description("Load a previously saved KNN model from Azure Blob Storage back into memory.")]
        public async Task<string> LoadModelFromBlob(
            [Description("Name of the saved model to load, e.g. 'knn-model-digit3'")] string modelName,
            [Description("Container name: 'train' or 'test'")] string containerName = "train")
        {
            try
            {
                string blobName = $"{modelName}.knn.json";
                var container = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                    throw new FileNotFoundException($"KNN model blob not found: {blobName}");

                var response = await blobClient.DownloadContentAsync();
                string json = response.Value.Content.ToString();

                var dto = JsonSerializer.Deserialize<List<KnnTrainingDto>>(json)
                          ?? new List<KnnTrainingDto>();

                _activeClassifier = new KnnClassifier();
                _activeClassifier.ImportTrainingData(dto);
                _activeModelName = modelName;

                CloudLogger.LogInfo("KnnClassifierTool", $"Loaded KNN model '{modelName}' with {dto.Count} examples");
                return $"✅ Loaded {dto.Count} KNN training examples from {containerName}/{blobName}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("KnnClassifierTool", "Error loading KNN model from blob", ex);
                throw;
            }
        }
    }
}