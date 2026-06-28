using System.ComponentModel;
using Azure.Storage.Blobs;
using Daenet.Binarizer;
using Daenet.Binarizer.Entities;
using ModelContextProtocol.Server;
using Azure.Storage.Blobs.Models;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class ImageProcessorTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string InputContainer = "input";
        private const string OutputContainer = "output";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageProcessing");

        public ImageProcessorTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // ============================================================
        // METHOD 1: Convert All Images in Azure Input Container
        // ============================================================
        [McpServerTool, Description("Download PNG images from Azure input container, binarize them, and upload results to output container.")]
        public async Task<string> ConvertImagesToBinary(
            [Description("Max number of images to process (default 100)")] int maxImages = 100)
        {
            try
            {
                var inputContainer = _blobServiceClient.GetBlobContainerClient(InputContainer);
                var outputContainer = _blobServiceClient.GetBlobContainerClient(OutputContainer);

                // Ensure output container exists
                await outputContainer.CreateIfNotExistsAsync();

                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in inputContainer.GetBlobsAsync())
                {
                    if (processed >= maxImages) break;

                    // Only process PNG files
                    if (!blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlob(blobItem.Name, inputContainer, outputContainer);
                        processed++;
                        Console.WriteLine($"✅ Processed: {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error processing {blobItem.Name}: {ex.Message}");
                        skipped++;
                    }
                }

                return $"✅ Done. Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Binarize a Single Image by Blob Name
        // ============================================================
        [McpServerTool, Description("Download a single PNG image from Azure, binarize it, and upload to output container.")]
        public async Task<string> BinarizeImage(
            [Description("Name of the PNG blob in the input container")] string blobName)
        {
            try
            {
                var inputContainer = _blobServiceClient.GetBlobContainerClient(InputContainer);
                var outputContainer = _blobServiceClient.GetBlobContainerClient(OutputContainer);

                await outputContainer.CreateIfNotExistsAsync();
                await ProcessSingleBlob(blobName, inputContainer, outputContainer);

                return $"✅ Successfully binarized: {blobName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error binarizing {blobName}: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 3: Process a Batch by Object Type
        // ============================================================
        [McpServerTool, Description("Binarize all images of a specific object type (e.g. '3' processes all 3_*.png files).")]
        public async Task<string> ProcessBatch(
            [Description("Object type prefix to filter (0-9 for digits)")] string objectType,
            [Description("Max number of images to process")] int maxImages = 100)
        {
            try
            {
                var inputContainer = _blobServiceClient.GetBlobContainerClient(InputContainer);
                var outputContainer = _blobServiceClient.GetBlobContainerClient(OutputContainer);

                await outputContainer.CreateIfNotExistsAsync();

                int processed = 0;
                int skipped = 0;

                await foreach (var blobItem in inputContainer.GetBlobsAsync()
                )
                {
                    if (processed >= maxImages) break;

                    if (!blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        await ProcessSingleBlob(blobItem.Name, inputContainer, outputContainer);
                        processed++;
                        Console.WriteLine($"✅ Processed: {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error processing {blobItem.Name}: {ex.Message}");
                        skipped++;
                    }
                }

                return $"✅ Batch done. Type: '{objectType}', Processed: {processed}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in batch: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // HELPER: Download → Binarize → Upload
        // ============================================================
        private async Task ProcessSingleBlob(
            string blobName,
            BlobContainerClient inputContainer,
            BlobContainerClient outputContainer)
        {
            // Build temp file paths
            string tempInputPath = Path.Combine(_tempFolder, blobName);
            string fileName = Path.GetFileNameWithoutExtension(blobName);
            string outputFileName = $"{fileName}_binarized.txt";
            string tempOutputPath = Path.Combine(_tempFolder, outputFileName);

            try
            {
                // Step 1 — Download PNG from Azure
                var inputBlob = inputContainer.GetBlobClient(blobName);
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

                // Step 3 — Upload result to output container
                var outputBlob = outputContainer.GetBlobClient(outputFileName);
                await outputBlob.UploadAsync(tempOutputPath, overwrite: true);
            }
            finally
            {
                // Cleanup temp files
                if (File.Exists(tempInputPath)) File.Delete(tempInputPath);
                if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
            }
        }
    }
}