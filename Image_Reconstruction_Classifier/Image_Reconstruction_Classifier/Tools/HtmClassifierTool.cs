using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageProcessing;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class HtmClassifierTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "HtmClassifier");

        // Trained classifiers, keyed by object type ("0".."9"). Kept in memory for the
        // lifetime of the tool instance (registered as a singleton in Program.cs).
        private readonly Dictionary<string, MyHtmClassifier> _classifiers = new();
        private int _trainingKeyCounter = 0;

        public HtmClassifierTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // ============================================================
        // METHOD 1: Train HTM Classifier on a Single Object Type
        // ============================================================
        [McpServerTool, Description("Train the HTM classifier for a specific object type (0-9) using spatial SDR blobs ('{type}_*_spatial.txt') paired with their matching binarized original images ('{type}_*_binarized.txt') from the container.")]
        public async Task<string> TrainClassifier(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Object type 0-9")] string objectType,
            [Description("Max number of training images to use (default 100)")] int maxImages = 100)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                if (!_classifiers.TryGetValue(objectType, out var classifier))
                {
                    classifier = new MyHtmClassifier();
                    _classifiers[objectType] = classifier;
                }

                int trained = 0;
                int skipped = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, $"{objectType}_", CancellationToken.None))
                {
                    if (trained >= maxImages) break;

                    if (!blobItem.Name.EndsWith("_spatial.txt", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        await TrainOnSingleBlob(blobItem.Name, containerClient, classifier);
                        trained++;
                        CloudLogger.LogDebug("HtmClassifierTool", $"Trained on: {blobItem.Name}");
                    }
                    catch (Exception ex)
                    {
                        CloudLogger.LogError("HtmClassifierTool", $"Error training on {blobItem.Name}", ex);
                        skipped++;
                    }
                }

                return $"✅ Training complete for type '{objectType}'. Trained: {trained}, Skipped: {skipped}";
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("HtmClassifierTool", "Error during HTM training", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Train All Object Types (0-9)
        // ============================================================
        [McpServerTool, Description("Train HTM classifiers for all object types (0-9) using the container's spatial SDR and binarized image blobs.")]
        public async Task<string> TrainAllObjectTypes(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Max number of training images per object type (default 100)")] int maxImagesPerType = 100)
        {
            var results = new List<string>();
            for (int type = 0; type < 10; type++)
            {
                string result = await TrainClassifier(containerName, type.ToString(), maxImagesPerType);
                results.Add(result);
            }

            return string.Join(" | ", results);
        }

        // ============================================================
        // METHOD 3: Reconstruct an Image from its Spatial SDR
        // ============================================================
        [McpServerTool, Description("Reconstruct an image from its spatial SDR blob using the trained HTM classifier for its object type, and upload the reconstructed pixel vector back to the container as '{name}_htm_reconstructed.txt'.")]
        public async Task<int[]> ReconstructImage(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Object type 0-9, must already be trained via TrainClassifier")] string objectType,
            [Description("Name of the spatial SDR blob, e.g. '3_552_spatial.txt'")] string spatialBlobName,
            [Description("Number of nearest training examples to use for reconstruction (default 3)")] int k = 3)
        {
            try
            {
                if (!_classifiers.TryGetValue(objectType, out var classifier))
                    throw new InvalidOperationException($"No trained classifier for object type '{objectType}'. Call TrainClassifier first.");

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                int[] sdr = await DownloadSdr(spatialBlobName, containerClient);

                int[] reconstructed = classifier.GetPredictedInputValues(sdr, k);

                string baseName = Path.GetFileNameWithoutExtension(spatialBlobName).Replace("_spatial", "");
                string outputBlobName = $"{baseName}_htm_reconstructed.txt";
                string tempOutputPath = Path.Combine(_tempFolder, outputBlobName);

                try
                {
                    File.WriteAllText(tempOutputPath, string.Join(",", reconstructed));

                    var outputBlob = containerClient.GetBlobClient(outputBlobName);
                    await outputBlob.UploadAsync(tempOutputPath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
                }

                CloudLogger.LogInfo("HtmClassifierTool", $"Reconstructed and uploaded: {outputBlobName}");
                return reconstructed;
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("HtmClassifierTool", "Error reconstructing image", ex);
                throw;
            }
        }

        // ============================================================
        // METHOD 4: List Object Types That Have a Trained Classifier
        // ============================================================
        [McpServerTool, Description("List the object types that currently have a trained HTM classifier in memory.")]
        public Task<List<string>> GetTrainedObjectTypes()
        {
            return Task.FromResult(_classifiers.Keys.OrderBy(k => k).ToList());
        }

        // ============================================================
        // HELPER: Train on a Single Spatial SDR Blob + its Matching Original
        // ============================================================
        private async Task TrainOnSingleBlob(
            string spatialBlobName,
            BlobContainerClient containerClient,
            MyHtmClassifier classifier)
        {
            int[] sdr = await DownloadSdr(spatialBlobName, containerClient);

            string originalBlobName = spatialBlobName.Replace("_spatial.txt", "_binarized.txt");
            string tempOriginalPath = Path.Combine(_tempFolder, originalBlobName);

            try
            {
                var originalBlob = containerClient.GetBlobClient(originalBlobName);
                if (!await originalBlob.ExistsAsync())
                    throw new FileNotFoundException($"Matching binarized image not found: {originalBlobName}");

                await originalBlob.DownloadToAsync(tempOriginalPath);
                int[] originalPixels = ImageLoader.LoadImage(tempOriginalPath);

                classifier.Learn(_trainingKeyCounter++, sdr, originalPixels);
            }
            finally
            {
                if (File.Exists(tempOriginalPath)) File.Delete(tempOriginalPath);
            }
        }

        // ============================================================
        // HELPER: Download and Parse a Comma-Separated SDR Blob
        // ============================================================
        private async Task<int[]> DownloadSdr(string spatialBlobName, BlobContainerClient containerClient)
        {
            string tempPath = Path.Combine(_tempFolder, spatialBlobName);
            try
            {
                var blobClient = containerClient.GetBlobClient(spatialBlobName);
                await blobClient.DownloadToAsync(tempPath);

                return File.ReadAllText(tempPath)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.Parse(s.Trim()))
                    .ToArray();
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
    }
}
