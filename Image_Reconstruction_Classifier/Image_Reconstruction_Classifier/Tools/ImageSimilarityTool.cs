using System.ComponentModel;
using Azure.Storage.Blobs;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class ImageSimilarityTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ResultStorageService _resultStorageService;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageSimilarity");

        public ImageSimilarityTool(
            BlobServiceClient blobServiceClient,
            ResultStorageService resultStorageService)
        {
            _blobServiceClient = blobServiceClient;
            _resultStorageService = resultStorageService;
            Directory.CreateDirectory(_tempFolder);
        }

        // ============================================================
        // METHOD 1: Calculate Cosine Similarity Between Two Images
        // ============================================================
        [McpServerTool, Description("Calculate the cosine similarity between an original and a reconstructed binary image. Returns a value between 0 and 1 where 1 means identical.")]
        public Task<double> CalculateCosineSimilarity(
            [Description("Flattened binary array of the original image")] int[] original,
            [Description("Flattened binary array of the reconstructed image")] int[] reconstructed)
        {
            try
            {
                if (original == null || reconstructed == null)
                    throw new ArgumentNullException("Image arrays cannot be null.");

                if (original.Length != reconstructed.Length)
                    throw new ArgumentException("Both images must have the same length.");

                double similarity = ImageSimilarity.CalculateCosineSimilarity(original, reconstructed);

                CloudLogger.LogInfo("ImageSimilarityTool", $"Cosine similarity: {similarity:F4}");
                return Task.FromResult(similarity);
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSimilarityTool", "Error calculating cosine similarity", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Calculate Binary (Pixel-wise) Similarity
        // ============================================================
        [McpServerTool, Description("Calculate the pixel-wise binary similarity between an original and a reconstructed image. Returns the percentage of matching pixels (0 to 100).")]
        public Task<double> CalculateBinarySimilarity(
            [Description("Flattened binary array of the original image")] int[] original,
            [Description("Flattened binary array of the reconstructed image")] int[] reconstructed)
        {
            try
            {
                if (original == null || reconstructed == null)
                    throw new ArgumentNullException("Image arrays cannot be null.");

                if (original.Length != reconstructed.Length)
                    throw new ArgumentException("Both images must have the same length.");

                int matchingPixels = original.Zip(reconstructed, (o, r) => o == r ? 1 : 0).Sum();
                double similarity = (double)matchingPixels / original.Length * 100.0;

                CloudLogger.LogInfo("ImageSimilarityTool", $"Binary similarity: {similarity:F2}%");
                return Task.FromResult(similarity);
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSimilarityTool", "Error calculating binary similarity", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 3: Compare Both Metrics at Once
        // ============================================================
        [McpServerTool, Description("Calculate both cosine and binary similarity in a single call, and save the result to Azure Table Storage. Returns a summary string.")]
        public async Task<string> CompareImages(
            [Description("Flattened binary array of the original image")] int[] original,
            [Description("Flattened binary array of the reconstructed image")] int[] reconstructed,
            [Description("Label or name for this image, e.g. '3_001'")] string imageName,
            [Description("Object type 0-9")] string objectType,
            [Description("Method used: 'HTM', 'KNN', or 'Combined'")] string method,
            [Description("Name of the reconstructed blob in Azure")] string reconstructedBlobName)
        {
            try
            {
                if (original == null || reconstructed == null)
                    throw new ArgumentNullException("Image arrays cannot be null.");

                if (original.Length != reconstructed.Length)
                    throw new ArgumentException("Both images must have the same length.");

                double cosineSimilarity = ImageSimilarity.CalculateCosineSimilarity(original, reconstructed);

                int matchingPixels = original.Zip(reconstructed, (o, r) => o == r ? 1 : 0).Sum();
                double binarySimilarity = (double)matchingPixels / original.Length * 100.0;

                // Save result to Azure Table Storage
                await _resultStorageService.SaveResultAsync(
                    imageName, objectType, method,
                    cosineSimilarity, binarySimilarity,
                    reconstructedBlobName);

                string result = $"Image: {imageName} | Cosine: {cosineSimilarity:F4} | Binary: {binarySimilarity:F2}%";
                CloudLogger.LogInfo("ImageSimilarityTool", result);
                return result;
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSimilarityTool", "Error comparing images", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 4: Batch Compare and Upload Results to Azure
        // ============================================================
        [McpServerTool, Description("Calculate similarity metrics for a batch and save each result to Azure Table Storage.")]
        public async Task<string> BatchCompareAndUpload(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Array of flattened binary original images")] int[][] originals,
            [Description("Array of flattened binary reconstructed images")] int[][] reconstructed,
            [Description("Array of image names")] string[] imageNames,
            [Description("Array of object types (0-9) matching each image")] string[] objectTypes,
            [Description("Method used: 'HTM', 'KNN', or 'Combined'")] string method,
            [Description("Array of reconstructed blob names")] string[] reconstructedBlobNames,
            [Description("Output blob name for CSV summary e.g. 'similarity_results.txt'")] string outputBlobName)
        {
            try
            {
                if (originals.Length != reconstructed.Length ||
                    originals.Length != imageNames.Length ||
                    originals.Length != objectTypes.Length ||
                    originals.Length != reconstructedBlobNames.Length)
                    throw new ArgumentException("All input arrays must be the same length.");

                var lines = new List<string>
                {
                    "Image Name,Object Type,Method,Cosine Similarity,Binary Similarity (%)"
                };

                for (int i = 0; i < originals.Length; i++)
                {
                    double cosine = ImageSimilarity.CalculateCosineSimilarity(originals[i], reconstructed[i]);
                    int matchingPixels = originals[i].Zip(reconstructed[i], (o, r) => o == r ? 1 : 0).Sum();
                    double binary = (double)matchingPixels / originals[i].Length * 100.0;

                    lines.Add($"{imageNames[i]},{objectTypes[i]},{method},{cosine:F4},{binary:F2}");
                    CloudLogger.LogDebug("ImageSimilarityTool", $"Compared {imageNames[i]}: Cosine={cosine:F4}, Binary={binary:F2}%");

                    // Save each result to Azure Table Storage
                    await _resultStorageService.SaveResultAsync(
                        imageNames[i], objectTypes[i], method,
                        cosine, binary, reconstructedBlobNames[i]);
                }

                string tempOutputPath = Path.Combine(_tempFolder, outputBlobName);
                try
                {
                    File.WriteAllLines(tempOutputPath, lines);

                    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                    var blobClient = containerClient.GetBlobClient(outputBlobName);
                    await blobClient.UploadAsync(tempOutputPath, overwrite: true);

                    return $"✅ Batch complete. {originals.Length} pairs compared. Results uploaded as '{outputBlobName}'";
                }
                finally
                {
                    if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
                }
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSimilarityTool", "Error in batch comparison", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 5: Convert Image to Binary Matrix String
        // ============================================================
        [McpServerTool, Description("Convert a flattened binary image array into a formatted 2D matrix string for visualization or debugging.")]
        public Task<string> ConvertToBinaryMatrix(
            [Description("Flattened binary image array")] int[] imageArray,
            [Description("Row size (width) of the image")] int rowSize)
        {
            try
            {
                if (imageArray == null)
                    throw new ArgumentNullException(nameof(imageArray), "Image array cannot be null.");

                string matrix = ImageSimilarity.ConvertToBinaryMatrix(imageArray, rowSize);

                CloudLogger.LogInfo("ImageSimilarityTool", $"Binary matrix generated for {rowSize}x{rowSize} image");
                return Task.FromResult(matrix);
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageSimilarityTool", "Error converting to binary matrix", ex);
                throw;
            }
        }
    }
}