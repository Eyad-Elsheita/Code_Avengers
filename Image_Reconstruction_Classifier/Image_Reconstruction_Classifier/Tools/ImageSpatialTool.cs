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

        // Placeholder — single image methods and helpers coming in next commit
        private Task ProcessSingleBlobTraining(string b, BlobContainerClient i, BlobContainerClient o, SpatialPooler sp) => Task.CompletedTask;
        private Task ProcessSingleBlobInference(string b, BlobContainerClient i, BlobContainerClient o, SpatialPooler sp) => Task.CompletedTask;
    }
}