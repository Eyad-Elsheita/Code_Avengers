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
    }
}