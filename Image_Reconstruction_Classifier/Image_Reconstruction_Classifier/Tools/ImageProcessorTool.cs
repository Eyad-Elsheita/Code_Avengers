using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Daenet.Binarizer;
using Daenet.Binarizer.Entities;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    /// <summary>
    /// MCP tool that binarizes PNG image blobs (via <see cref="Daenet.Binarizer.ImageBinarizer"/>)
    /// and uploads the resulting binarized pixel text back to Azure Blob Storage.
    /// </summary>
    [McpServerToolType]
    public class ImageProcessorTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageProcessing");

        /// <summary>
        /// Creates the tool with the Azure Blob Storage client used to read and write image blobs.
        /// </summary>
        /// <param name="blobServiceClient">Client for the storage account holding the 'train'/'test' containers.</param>
        public ImageProcessorTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // ============================================================
        // METHOD 1: Convert All Images in Container
        // ============================================================
        /// <summary>
        /// Binarizes every PNG blob in the container (up to <paramref name="maxImages"/>) and
        /// uploads each result as '{name}_binarized.txt'.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="maxImages">Maximum number of images to process.</param>
        /// <returns>A summary string with processed/skipped counts.</returns>
        [McpServerTool, Description("Download PNG images from Azure container, binarize them, and upload results back to same container.")]
        public async Task<string> ConvertImagesToBinary(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Max number of images to process (default 100)")] int maxImages = 100)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, null, CancellationToken.None))
                {
                    if (processed >= maxImages) break;

                    if (!blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlob(blobItem.Name, containerClient);
                        processed++;
                        CloudLogger.LogDebug("ImageProcessorTool", $"Processed: {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        CloudLogger.LogError("ImageProcessorTool", $"Error processing {blobItem.Name}", ex);
                        skipped++;
                    }
                }

                return $"✅ Done. Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageProcessorTool", "Error", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Binarize a Single Image by Blob Name
        // ============================================================
        /// <summary>
        /// Binarizes a single named PNG blob and uploads the result as '{name}_binarized.txt'.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="blobName">Name of the PNG blob, e.g. '3_552.png'.</param>
        /// <returns>A success message naming the binarized blob.</returns>
        [McpServerTool, Description("Download a single PNG image from Azure, binarize it, and upload back to same container.")]
        public async Task<string> BinarizeImage(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Name of the PNG blob e.g. '3_552.png'")] string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await ProcessSingleBlob(blobName, containerClient);
                return $"✅ Successfully binarized: {blobName}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageProcessorTool", $"Error binarizing {blobName}", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 3: Process a Batch by Object Type
        // ============================================================
        /// <summary>
        /// Binarizes every PNG blob whose name is prefixed with <paramref name="objectType"/>
        /// (up to <paramref name="maxImages"/>).
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="objectType">Object type prefix to filter by, e.g. '3' matches '3_*.png'.</param>
        /// <param name="maxImages">Maximum number of images to process.</param>
        /// <returns>A summary string with the object type and processed/skipped counts.</returns>
        [McpServerTool, Description("Binarize all images of a specific object type (e.g. '3' processes all 3_*.png files).")]
        public async Task<string> ProcessBatch(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Object type prefix to filter (0-9 for digits)")] string objectType,
            [Description("Max number of images to process")] int maxImages = 100)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, $"{objectType}_", CancellationToken.None))
                {
                    if (processed >= maxImages) break;

                    if (!blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlob(blobItem.Name, containerClient);
                        processed++;
                        CloudLogger.LogDebug("ImageProcessorTool", $"Processed: {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        CloudLogger.LogError("ImageProcessorTool", $"Error processing {blobItem.Name}", ex);
                        skipped++;
                    }
                }

                return $"✅ Batch done. Type: '{objectType}', Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageProcessorTool", "Error in batch", ex);
                throw;
            }
        }

        // ============================================================
        // HELPER: Download → Binarize → Upload back to same container
        // ============================================================
        private async Task ProcessSingleBlob(
            string blobName,
            BlobContainerClient containerClient)
        {
            string tempInputPath = Path.Combine(_tempFolder, blobName);
            string fileName = Path.GetFileNameWithoutExtension(blobName);
            string outputFileName = $"{fileName}_binarized.txt";
            string tempOutputPath = Path.Combine(_tempFolder, outputFileName);

            try
            {
                // Step 1 — Download PNG from Azure
                var inputBlob = containerClient.GetBlobClient(blobName);
                await inputBlob.DownloadToAsync(tempInputPath);

                // Step 2 — Binarize using existing ImageBinarizer
                var binarizerParams = new BinarizerParams
                {
                    InputImagePath = tempInputPath,
                    OutputImagePath = tempOutputPath,
                    GreyScale = true,
                    CreateCode = false
                };

                var imageBinarizer = new ImageBinarizer(binarizerParams);
                imageBinarizer.Run();

                // Step 3 — Upload binarized result back to same container
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