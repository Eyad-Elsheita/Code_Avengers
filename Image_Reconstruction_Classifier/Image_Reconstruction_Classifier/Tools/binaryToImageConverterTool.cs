using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class BinaryToImageConverterTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageReconstruction");

        public BinaryToImageConverterTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // ============================================================
        // METHOD 1: Convert Binary Array to PNG and Upload to Azure
        // ============================================================
        [McpServerTool, Description("Convert a binary (0/1) integer array into a PNG image and upload it back to the same container.")]
        public async Task<string> ConvertBinaryToImage(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Flattened binary array (0s and 1s) representing the image")] int[] binaryImage,
            [Description("Width of the image in pixels")] int width,
            [Description("Height of the image in pixels")] int height,
            [Description("Output blob name, e.g. '3_001_reconstructed.png'")] string outputBlobName)
        {
            try
            {
                if (binaryImage == null)
                    throw new ArgumentNullException(nameof(binaryImage), "The binary image array cannot be null.");

                if (binaryImage.Length != width * height)
                    throw new ArgumentException("The length of the binary image array must match width × height.");

                string tempOutputPath = Path.Combine(_tempFolder, outputBlobName);

                try
                {
                    // Step 1 — Reconstruct PNG locally
                    BinaryToImageConverter.SaveBinaryAsPng(binaryImage, width, height, tempOutputPath);

                    // Step 2 — Upload back to same container
                    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                    var blobClient = containerClient.GetBlobClient(outputBlobName);
                    await blobClient.UploadAsync(tempOutputPath, overwrite: true);

                    Console.WriteLine($"✅ Reconstructed image uploaded: {outputBlobName}");
                    return $"✅ Image saved to '{containerName}' as '{outputBlobName}'";
                }
                finally
                {
                    if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error converting binary to image: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Save Reconstructed Image with naming convention
        // ============================================================
        [McpServerTool, Description("Reconstruct and save an image following the 'label_index_reconstructed.png' naming convention.")]
        public async Task<string> SaveReconstructedImage(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Flattened binary array (0s and 1s)")] int[] binaryImage,
            [Description("Width of the image")] int width,
            [Description("Height of the image")] int height,
            [Description("Label / object type, e.g. '3'")] string label,
            [Description("Image index, e.g. '001'")] string index)
        {
            string outputBlobName = $"{label}_{index}_reconstructed.png";
            return await ConvertBinaryToImage(containerName, binaryImage, width, height, outputBlobName);
        }

        // ============================================================
        // METHOD 3: Batch Convert Multiple Binary Images
        // ============================================================
        [McpServerTool, Description("Convert multiple binary arrays into PNG images and upload them all to the same container.")]
        public async Task<string> BatchConvert(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Array of flattened binary images")] int[][] binaryImages,
            [Description("Width of each image")] int width,
            [Description("Height of each image")] int height,
            [Description("Prefix for output filenames, e.g. 'reconstructed'")] string filePrefix)
        {
            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < binaryImages.Length; i++)
            {
                try
                {
                    string outputBlobName = $"{filePrefix}_{i:D4}.png";
                    await ConvertBinaryToImage(containerName, binaryImages[i], width, height, outputBlobName);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed image {i}: {ex.Message}");
                    failCount++;
                }
            }

            return $"✅ Batch complete. Success: {successCount}, Failed: {failCount}";
        }
    }
}