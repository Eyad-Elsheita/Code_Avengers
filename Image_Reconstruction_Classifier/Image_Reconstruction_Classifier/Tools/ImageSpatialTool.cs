using System.ComponentModel;
using Azure.Storage.Blobs;
using ModelContextProtocol.Server;
using NeoCortexApi;
using NeoCortexApi.Entities;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class ImageSpatialTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string InputContainer = "input";
        private const string OutputContainer = "output";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageSpatial");

        public ImageSpatialTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // Shared HTM Spatial Pooler initialization to avoid duplication
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
        // METHOD 1: Run Spatial Pooler Training on Azure Blob Input
        // ============================================================
        [McpServerTool, Description("Download encoded image vectors from Azure input container, run them through the HTM Spatial Pooler in training mode, and upload the resulting SDRs to the output container.")]
        public async Task<string> TrainSpatialPooler(
            [Description("Max number of images to process (default 100)")] int maxImages = 100)
        {
            try
            {
                var inputContainer = _blobServiceClient.GetBlobContainerClient(InputContainer);
                var outputContainer = _blobServiceClient.GetBlobContainerClient(OutputContainer);

                await outputContainer.CreateIfNotExistsAsync();

                var (spatialPooler, _) = InitializeSpatialPooler();

                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in inputContainer.GetBlobsAsync())
                {
                    if (processed >= maxImages) break;

                    if (!blobItem.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlobTraining(blobItem.Name, inputContainer, outputContainer, spatialPooler);
                        processed++;
                        Console.WriteLine($"Successfully processed (training): {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {blobItem.Name}: {ex.Message}");
                        skipped++;
                    }
                }

                return $"Training complete. Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during spatial pooler training: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Run Spatial Pooler Inference on Azure Blob Input
        // ============================================================
        [McpServerTool, Description("Download encoded test image vectors from Azure input container, run them through the HTM Spatial Pooler in inference mode (no learning), and upload the resulting SDRs to the output container.")]
        public async Task<string> RunSpatialPoolerInference(
            [Description("Max number of test images to process (default 100)")] int maxImages = 100)
        {
            try
            {
                var inputContainer = _blobServiceClient.GetBlobContainerClient(InputContainer);
                var outputContainer = _blobServiceClient.GetBlobContainerClient(OutputContainer);

                await outputContainer.CreateIfNotExistsAsync();

                var (spatialPooler, _) = InitializeSpatialPooler();

                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in inputContainer.GetBlobsAsync())
                {
                    if (processed >= maxImages) break;

                    if (!blobItem.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlobInference(blobItem.Name, inputContainer, outputContainer, spatialPooler);
                        processed++;
                        Console.WriteLine($"Successfully processed (inference): {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {blobItem.Name}: {ex.Message}");
                        skipped++;
                    }
                }

                return $"Inference complete. Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during spatial pooler inference: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 3: Process a Single Image by Blob Name (Training)
        // ============================================================
        [McpServerTool, Description("Run a single encoded image vector through the HTM Spatial Pooler in training mode and upload the resulting SDR to the output container.")]
        public async Task<string> TrainSingleImage(
            [Description("Name of the .txt blob in the input container")] string blobName)
        {
            try
            {
                var inputContainer = _blobServiceClient.GetBlobContainerClient(InputContainer);
                var outputContainer = _blobServiceClient.GetBlobContainerClient(OutputContainer);

                await outputContainer.CreateIfNotExistsAsync();

                var (spatialPooler, _) = InitializeSpatialPooler();
                await ProcessSingleBlobTraining(blobName, inputContainer, outputContainer, spatialPooler);

                return $"Successfully trained on: {blobName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error training on {blobName}: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 4: Process a Single Image by Blob Name (Inference)
        // ============================================================
        [McpServerTool, Description("Run a single encoded test image vector through the HTM Spatial Pooler in inference mode and upload the resulting SDR to the output container.")]
        public async Task<string> InferSingleImage(
            [Description("Name of the .txt blob in the input container")] string blobName)
        {
            try
            {
                var inputContainer = _blobServiceClient.GetBlobContainerClient(InputContainer);
                var outputContainer = _blobServiceClient.GetBlobContainerClient(OutputContainer);

                await outputContainer.CreateIfNotExistsAsync();

                var (spatialPooler, _) = InitializeSpatialPooler();
                await ProcessSingleBlobInference(blobName, inputContainer, outputContainer, spatialPooler);

                return $"Successfully ran inference on: {blobName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during inference on {blobName}: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // HELPER: Download → Spatial Pool (Training) → Upload
        // ============================================================
        private async Task ProcessSingleBlobTraining(
            string blobName,
            BlobContainerClient inputContainer,
            BlobContainerClient outputContainer,
            SpatialPooler spatialPooler)
        {
            string tempInputPath = Path.Combine(_tempFolder, blobName);
            string fileName = Path.GetFileNameWithoutExtension(blobName);
            string[] nameParts = fileName.Split('_');
            string outputFileName = $"{nameParts[0]}_{nameParts[1]}_spatial.txt";
            string tempOutputPath = Path.Combine(_tempFolder, outputFileName);

            try
            {
                // Step 1 — Download encoded vector from Azure
                var inputBlob = inputContainer.GetBlobClient(blobName);
                await inputBlob.DownloadToAsync(tempInputPath);

                // Step 2 — Parse and validate input vector
                string rawData = File.ReadAllText(tempInputPath).Trim();
                if (string.IsNullOrWhiteSpace(rawData))
                    throw new InvalidDataException($"Empty file: {blobName}");

                int[] inputVector = rawData.Split(',')
                    .Select(int.Parse)
                    .ToArray();

                if (inputVector.Length != 784)
                    throw new InvalidDataException($"Dimension mismatch in {fileName}: Expected 784, got {inputVector.Length}");

                // Step 3 — Run Spatial Pooler in training mode
                int[] activeColumns = spatialPooler.Compute(inputVector, learn: true);

                // Step 4 — Save SDR result and upload to Azure
                File.WriteAllText(tempOutputPath, string.Join(",", activeColumns));
                var outputBlob = outputContainer.GetBlobClient(outputFileName);
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
            BlobContainerClient inputContainer,
            BlobContainerClient outputContainer,
            SpatialPooler spatialPooler)
        {
            string tempInputPath = Path.Combine(_tempFolder, blobName);
            string fileName = Path.GetFileNameWithoutExtension(blobName);
            string[] nameParts = fileName.Split('_');
            string outputFileName = $"{nameParts[0]}_{nameParts[1]}_spatial.txt";
            string tempOutputPath = Path.Combine(_tempFolder, outputFileName);

            try
            {
                // Step 1 — Download encoded vector from Azure
                var inputBlob = inputContainer.GetBlobClient(blobName);
                await inputBlob.DownloadToAsync(tempInputPath);

                // Step 2 — Parse and validate input vector
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

                // Step 3 — Run Spatial Pooler in inference mode (no learning)
                int[] activeColumns = spatialPooler.Compute(inputVector, learn: false);

                if (activeColumns.Length == 0)
                    throw new InvalidOperationException($"Zero active columns produced for {fileName}");

                // Step 4 — Save SDR result and upload to Azure
                File.WriteAllText(tempOutputPath, string.Join(",", activeColumns));
                var outputBlob = outputContainer.GetBlobClient(outputFileName);
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