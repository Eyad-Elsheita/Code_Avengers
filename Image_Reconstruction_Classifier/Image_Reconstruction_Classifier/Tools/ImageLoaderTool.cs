using System.ComponentModel;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageProcessing;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class ImageLoaderTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string TrainContainer = "train";
        private const string TestContainer = "test";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageLoader");

        public ImageLoaderTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // ============================================================
        // METHOD 1: Load Multiple Images from Azure Blob
        // ============================================================
        [McpServerTool, Description("Load images from Azure Blob Storage. Use 'train' for training images or 'test' for test images.")]
        public async Task<int[][]> LoadImagesFromBlob(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("How many images to load (max 10000)")] int numberOfImages)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var imageList = new List<int[]>();
                int count = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, prefix: null))
                {
                    if (count >= numberOfImages) break;

                    // Only load PNG files
                    if (!blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string tempPath = Path.Combine(_tempFolder, blobItem.Name);
                    try
                    {
                        var blobClient = containerClient.GetBlobClient(blobItem.Name);
                        await blobClient.DownloadToAsync(tempPath);

                        int[] imageData = await Task.Run(() => ImageLoader.LoadImage(tempPath));
                        imageList.Add(imageData);
                        count++;
                    }
                    finally
                    {
                        if (File.Exists(tempPath)) File.Delete(tempPath);
                    }
                }

                Console.WriteLine($"✅ Loaded {imageList.Count} images from '{containerName}'");
                return imageList.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading images: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Load Single Image from Azure Blob
        // ============================================================
        [McpServerTool, Description("Load a single image from Azure Blob Storage by name.")]
        public async Task<int[]> LoadSingleImageFromBlob(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Blob name e.g. '3_552.png'")] string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                    throw new FileNotFoundException($"Blob not found: {blobName}");

                string tempPath = Path.Combine(_tempFolder, blobName);
                try
                {
                    await blobClient.DownloadToAsync(tempPath);
                    int[] imageData = await Task.Run(() => ImageLoader.LoadImage(tempPath));

                    Console.WriteLine($"✅ Loaded single image: {blobName}");
                    return imageData;
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading single image: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 3: List Available Images in Container
        // ============================================================
        [McpServerTool, Description("List all PNG image blobs in a container.")]
        public async Task<List<string>> ListAvailableImages(
            [Description("Container name: 'train' or 'test'")] string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var fileList = new List<string>();

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, prefix: null))
                {
                    if (blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        fileList.Add(blobItem.Name);
                }

                Console.WriteLine($"✅ Found {fileList.Count} images in '{containerName}'");
                return fileList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing images: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 4: Get Image Count in Container
        // ============================================================
        [McpServerTool, Description("Get count of PNG images in a container.")]
        public async Task<int> GetImageCount(
            [Description("Container name: 'train' or 'test'")] string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                int count = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, prefix: null))
                {
                    if (blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        count++;
                }

                Console.WriteLine($"✅ Found {count} images in '{containerName}'");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting image count: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 5: Load Images by Object Type from Azure Blob
        // ============================================================
        [McpServerTool, Description("Load images filtered by object type (0-9) from Azure Blob. E.g. objectType '3' loads all 3_*.png files.")]
        public async Task<int[][]> LoadImagesByObjectType(
            [Description("Container name: 'train' or 'test'")] string containerName,
            [Description("Object type 0-9")] string objectType,
            [Description("How many images to load")] int numberOfImages)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var imageList = new List<int[]>();
                int count = 0;

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    BlobTraits.None, BlobStates.All, prefix: $"{objectType}_"))
                {
                    if (count >= numberOfImages) break;

                    if (!blobItem.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string tempPath = Path.Combine(_tempFolder, blobItem.Name);
                    try
                    {
                        var blobClient = containerClient.GetBlobClient(blobItem.Name);
                        await blobClient.DownloadToAsync(tempPath);

                        int[] imageData = await Task.Run(() => ImageLoader.LoadImage(tempPath));
                        imageList.Add(imageData);
                        count++;
                    }
                    finally
                    {
                        if (File.Exists(tempPath)) File.Delete(tempPath);
                    }
                }

                Console.WriteLine($"✅ Loaded {imageList.Count} images of type '{objectType}' from '{containerName}'");
                return imageList.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading filtered images: {ex.Message}");
                throw;
            }
        }
    }
}