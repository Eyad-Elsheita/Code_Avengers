using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    /// <summary>
    /// MCP tool that renders flattened binary (0/1) pixel arrays back into PNG images and
    /// uploads them to Azure Blob Storage.
    /// </summary>
    [McpServerToolType]
    public class BinaryToImageConverterTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageReconstruction");

        /// <summary>
        /// Creates the tool with the Azure Blob Storage client used to write rendered images.
        /// </summary>
        /// <param name="blobServiceClient">Client for the storage account holding the 'train'/'test' containers.</param>
        public BinaryToImageConverterTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // ============================================================
        // METHOD 1: Convert Binary Array to PNG and Upload to Azure
        // ============================================================
        /// <summary>
        /// Renders a flattened binary pixel array as a PNG and uploads it to the container as
        /// <paramref name="outputBlobName"/>.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="binaryImage">Flattened binary array (0s and 1s) representing the image.</param>
        /// <param name="width">Width of the image in pixels.</param>
        /// <param name="height">Height of the image in pixels.</param>
        /// <param name="outputBlobName">Output blob name, e.g. '3_001_reconstructed.png'.</param>
        /// <returns>A success message naming the container and output blob.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="binaryImage"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the array length does not equal width × height.</exception>
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

                    CloudLogger.LogInfo("BinaryToImageConverterTool", $"Reconstructed image uploaded: {outputBlobName}");
                    return $"✅ Image saved to '{containerName}' as '{outputBlobName}'";
                }
                finally
                {
                    if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
                }
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("BinaryToImageConverterTool", "Error converting binary to image", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Save Reconstructed Image with naming convention
        // ============================================================
        /// <summary>
        /// Renders and uploads an image via <see cref="ConvertBinaryToImage"/>, naming the output
        /// blob '{label}_{index}_reconstructed.png'.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="binaryImage">Flattened binary array (0s and 1s).</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="label">Label / object type, e.g. '3'.</param>
        /// <param name="index">Image index, e.g. '001'.</param>
        /// <returns>A success message naming the container and output blob.</returns>
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
        /// <summary>
        /// Renders and uploads each binary image in <paramref name="binaryImages"/> via
        /// <see cref="ConvertBinaryToImage"/>, naming outputs '{filePrefix}_{index:D4}.png'.
        /// Per-image failures are counted rather than aborting the batch.
        /// </summary>
        /// <param name="containerName">Container name: 'train' or 'test'.</param>
        /// <param name="binaryImages">Array of flattened binary images.</param>
        /// <param name="width">Width of each image.</param>
        /// <param name="height">Height of each image.</param>
        /// <param name="filePrefix">Prefix for output filenames, e.g. 'reconstructed'.</param>
        /// <returns>A summary string with success/failure counts.</returns>
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
                    CloudLogger.LogError("BinaryToImageConverterTool", $"Failed image {i}", ex);
                    failCount++;
                }
            }

            return $"✅ Batch complete. Success: {successCount}, Failed: {failCount}";
        }
    }
}