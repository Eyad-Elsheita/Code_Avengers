using System.ComponentModel;
using Azure.Storage.Blobs;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class ImageSimilarityTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string OutputContainer = "output";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageSimilarity");

        public ImageSimilarityTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
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

                Console.WriteLine($"Cosine similarity calculated: {similarity:F4}");
                return Task.FromResult(similarity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating cosine similarity: {ex.Message}");
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

                Console.WriteLine($"Binary similarity calculated: {similarity:F2}%");
                return Task.FromResult(similarity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating binary similarity: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 3: Compare Both Metrics at Once
        // ============================================================
        [McpServerTool, Description("Calculate both cosine similarity and binary pixel similarity between an original and reconstructed image in a single call. Returns a summary string.")]
        public Task<string> CompareImages(
            [Description("Flattened binary array of the original image")] int[] original,
            [Description("Flattened binary array of the reconstructed image")] int[] reconstructed,
            [Description("Label or name for this image, e.g. 'digit_3_001'")] string imageName)
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

                string result = $"Image: {imageName} | Cosine Similarity: {cosineSimilarity:F4} | Binary Similarity: {binarySimilarity:F2}%";
                Console.WriteLine(result);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error comparing images: {ex.Message}");
                throw;
            }
        }
    }
}