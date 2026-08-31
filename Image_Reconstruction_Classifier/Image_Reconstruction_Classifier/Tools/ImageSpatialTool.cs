using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ModelContextProtocol.Server;
using NeoCortexApi;
using NeoCortexApi.Entities;

namespace Image_Reconstruction_Classifier.Tools
{
    /// <summary>
    /// MCP tool that runs encoded 784-element image vectors through an HTM
    /// <see cref="SpatialPooler"/> (training or inference mode) and uploads the resulting
    /// SDR (active column indices) to Azure Blob Storage. Each call initializes a fresh,
    /// untrained pooler, so results are only meaningful within a single call.
    /// </summary>
    [McpServerToolType]
    public class ImageSpatialTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageSpatial");

        /// <summary>
        /// Creates the tool with the Azure Blob Storage client used to read and write blobs.
        /// </summary>
        /// <param name="blobServiceClient">Client for the storage account holding the 'train'/'test' containers.</param>
        public ImageSpatialTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // Shared HTM Spatial Pooler initialization — UNCHANGED
        private static (SpatialPooler, Connections) InitializeSpatialPooler()
        {
            SpatialPooler spatialPooler = new SpatialPooler();
            Connections connections = new Connections();

            connections.HtmConfig.InputDimensions = new int[] { 784 };
            connections.HtmConfig.ColumnDimensions = new int[] { 2048 };
            connections.HtmConfig.PotentialRadius = 12;
            connections.HtmConfig.PotentialPct = 0.8;
            connections.HtmConfig.GlobalInhibition = true;
            connections.HtmConfig.LocalAreaDensity = 0.03;
            connections.HtmConfig.StimulusThreshold = 5;
            connections.HtmConfig.SynPermInactiveDec = 0.008;
            connections.HtmConfig.SynPermActiveInc = 0.05;
            connections.HtmConfig.SynPermConnected = 0.2;

            spatialPooler.Init(connections);
            return (spatialPooler, connections);
        }

        // ============================================================
        // METHOD 1: Train Spatial Pooler on Container
        // ============================================================
        /// <summary>
        /// Downloads up to <paramref name="maxImages"/> encoded '.txt' vectors from the container,
        /// runs each through one shared spatial pooler in training mode (<c>learn: true</c>), and
        /// uploads each result as '{name}_spatial.txt'.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="maxImages">Maximum number of images to process.</param>
        /// <returns>A summary string with processed/skipped counts.</returns>
        [McpServerTool, Description("Download encoded image vectors from Azure input container, run them through the HTM Spatial Pooler in training mode, and upload the resulting SDRs to the output container.")]
        public async Task<string> TrainSpatialPooler(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Max number of images to process (default 100)")] int maxImages = 100)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var (spatialPooler, _) = InitializeSpatialPooler();

                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, null, CancellationToken.None))
                {
                    if (processed >= maxImages) break;

                    if (!blobItem.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlobTraining(blobItem.Name, containerClient, spatialPooler);
                        processed++;
                        CloudLogger.LogDebug("ImageSpatialTool", $"Successfully processed (training): {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        CloudLogger.LogError("ImageSpatialTool", $"Error processing {blobItem.Name}", ex);
                        skipped++;
                    }
                }

                return $"Training complete. Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSpatialTool", "Error during spatial pooler training", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Run Spatial Pooler Inference on Container
        // ============================================================
        /// <summary>
        /// Downloads up to <paramref name="maxImages"/> encoded '.txt' vectors from the container,
        /// runs each through one shared spatial pooler in inference mode (<c>learn: false</c>), and
        /// uploads each result as '{name}_spatial.txt'.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="maxImages">Maximum number of test images to process.</param>
        /// <returns>A summary string with processed/skipped counts.</returns>
        [McpServerTool, Description("Download encoded test image vectors from Azure input container, run them through the HTM Spatial Pooler in inference mode (no learning), and upload the resulting SDRs to the output container.")]
        public async Task<string> RunSpatialPoolerInference(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Max number of test images to process (default 100)")] int maxImages = 100)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var (spatialPooler, _) = InitializeSpatialPooler();

                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, null, CancellationToken.None))
                {
                    if (processed >= maxImages) break;

                    if (!blobItem.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlobInference(blobItem.Name, containerClient, spatialPooler);
                        processed++;
                        CloudLogger.LogDebug("ImageSpatialTool", $"Successfully processed (inference): {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        CloudLogger.LogError("ImageSpatialTool", $"Error processing {blobItem.Name}", ex);
                        skipped++;
                    }
                }

                return $"Inference complete. Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSpatialTool", "Error during spatial pooler inference", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 3: Train Single Image
        // ============================================================
        /// <summary>
        /// Runs a single encoded '.txt' vector through a fresh spatial pooler in training mode
        /// (<c>learn: true</c>) and uploads the resulting SDR as '{name}_spatial.txt'.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="blobName">Name of the encoded '.txt' blob in the container.</param>
        /// <returns>A success message naming the source blob.</returns>
        /// <exception cref="InvalidDataException">Thrown when the vector is empty or not exactly 784 elements.</exception>
        [McpServerTool, Description("Run a single encoded image vector through the HTM Spatial Pooler in training mode and upload the resulting SDR to the output container.")]
        public async Task<string> TrainSingleImage(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Name of the .txt blob in the input container")] string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var (spatialPooler, _) = InitializeSpatialPooler();
                await ProcessSingleBlobTraining(blobName, containerClient, spatialPooler);
                return $"Successfully trained on: {blobName}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSpatialTool", $"Error training on {blobName}", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 4: Infer Single Image
        // ============================================================
        /// <summary>
        /// Runs a single encoded '.txt' vector through a fresh spatial pooler in inference mode
        /// (<c>learn: false</c>) and uploads the resulting SDR as '{name}_spatial.txt'.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="blobName">Name of the encoded '.txt' blob in the container.</param>
        /// <returns>A success message naming the source blob.</returns>
        /// <exception cref="InvalidDataException">Thrown when the vector is empty or not exactly 784 elements.</exception>
        /// <exception cref="InvalidOperationException">Thrown when inference produces zero active columns.</exception>
        [McpServerTool, Description("Run a single encoded test image vector through the HTM Spatial Pooler in inference mode and upload the resulting SDR to the output container.")]
        public async Task<string> InferSingleImage(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Name of the .txt blob in the input container")] string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var (spatialPooler, _) = InitializeSpatialPooler();
                await ProcessSingleBlobInference(blobName, containerClient, spatialPooler);
                return $"Successfully ran inference on: {blobName}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSpatialTool", $"Error during inference on {blobName}", ex);
                throw;
            }
        }

        // ============================================================
        // HELPER: Download → Spatial Pool (Training) → Upload
        // ============================================================
        private async Task ProcessSingleBlobTraining(
            string blobName,
            BlobContainerClient containerClient,
            SpatialPooler spatialPooler)
        {
            string tempInputPath = Path.Combine(_tempFolder, blobName);
            string fileName = Path.GetFileNameWithoutExtension(blobName);
            string[] nameParts = fileName.Split('_');
            string outputFileName = $"{nameParts[0]}_{nameParts[1]}_spatial.txt";
            string tempOutputPath = Path.Combine(_tempFolder, outputFileName);

            try
            {
                var inputBlob = containerClient.GetBlobClient(blobName);
                await inputBlob.DownloadToAsync(tempInputPath);

                string rawData = File.ReadAllText(tempInputPath).Trim();
                if (string.IsNullOrWhiteSpace(rawData))
                    throw new InvalidDataException($"Empty file: {blobName}");

                int[] inputVector = rawData.Split(',')
                    .Select(int.Parse)
                    .ToArray();

                if (inputVector.Length != 784)
                    throw new InvalidDataException($"Dimension mismatch in {fileName}: Expected 784, got {inputVector.Length}");

                int[] activeColumns = spatialPooler.Compute(inputVector, learn: true);

                File.WriteAllText(tempOutputPath, string.Join(",", activeColumns));
                var outputBlob = containerClient.GetBlobClient(outputFileName);
                await outputBlob.UploadAsync(tempOutputPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempInputPath)) File.Delete(tempInputPath);
                if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
            }
        }

        // ============================================================
        // HELPER: Download → Spatial Pool (Inference) → Upload
        // ============================================================
        private async Task ProcessSingleBlobInference(
            string blobName,
            BlobContainerClient containerClient,
            SpatialPooler spatialPooler)
        {
            string tempInputPath = Path.Combine(_tempFolder, blobName);
            string fileName = Path.GetFileNameWithoutExtension(blobName);
            string[] nameParts = fileName.Split('_');
            string outputFileName = $"{nameParts[0]}_{nameParts[1]}_spatial.txt";
            string tempOutputPath = Path.Combine(_tempFolder, outputFileName);

            try
            {
                var inputBlob = containerClient.GetBlobClient(blobName);
                await inputBlob.DownloadToAsync(tempInputPath);

                string rawData = File.ReadAllText(tempInputPath).Trim();
                if (string.IsNullOrWhiteSpace(rawData))
                    throw new InvalidDataException($"Empty file: {blobName}");

                int[] inputVector = rawData
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int num) ? num : -1)
                    .Where(n => n >= 0)
                    .ToArray();

                if (inputVector.Length != 784)
                    throw new InvalidDataException($"Invalid dimensionality in {fileName}: {inputVector.Length}/784");

                int[] activeColumns = spatialPooler.Compute(inputVector, learn: false);

                if (activeColumns.Length == 0)
                    throw new InvalidOperationException($"Zero active columns produced for {fileName}");

                File.WriteAllText(tempOutputPath, string.Join(",", activeColumns));
                var outputBlob = containerClient.GetBlobClient(outputFileName);
                await outputBlob.UploadAsync(tempOutputPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempInputPath)) File.Delete(tempInputPath);
                if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
            }
        }
    }
}