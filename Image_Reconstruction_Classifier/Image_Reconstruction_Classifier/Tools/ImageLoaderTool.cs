using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ImageProcessing;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class ImageLoaderTool
    {
        // ============================================================
        // METHOD 1: Load Images from Local Filesystem
        // ============================================================
        [McpServerTool, Description("Load images from local filesystem folder. Returns flattened image arrays.")]
        public async Task<int[][]> LoadImagesFromLocal(
            [Description("Path to folder containing image files")] string folderPath,
            [Description("How many images to load (max 10000)")] int numberOfImages)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

                if (numberOfImages <= 0 || numberOfImages > 10000)
                    throw new ArgumentException("numberOfImages must be between 1 and 10000");

                int[][] imageData = await Task.Run(() =>
                    ImageLoader.LoadImageData(folderPath, numberOfImages)
                );

                Console.WriteLine($"✅ Loaded {imageData.Length} images from {folderPath}");
                return imageData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading images: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 2: Load Single Image from Local Filesystem
        // ============================================================
        [McpServerTool, Description("Load a single image file from local filesystem. Returns as flattened integer array.")]
        public async Task<int[]> LoadSingleImageFromLocal(
            [Description("Full path to the image file")] string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                int[] imageData = await Task.Run(() =>
                    ImageLoader.LoadImage(filePath)
                );

                Console.WriteLine($"✅ Loaded single image: {Path.GetFileName(filePath)}");
                return imageData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading image: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 3: List Available Images in Folder
        // ============================================================
        [McpServerTool, Description("List all available image files in a folder.")]
        public async Task<List<string>> ListAvailableImages(
            [Description("Path to folder containing images")] string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

                var fileList = await Task.Run(() =>
                    Directory.GetFiles(folderPath, "*.txt")
                        .Select(Path.GetFileName)
                        .ToList()
                );

                Console.WriteLine($"✅ Found {fileList.Count} image files in {folderPath}");
                return fileList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing images: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 4: Get Image Count
        // ============================================================
        [McpServerTool, Description("Get count of image files in a folder.")]
        public async Task<int> GetImageCount(
            [Description("Path to folder")] string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

                int count = await Task.Run(() =>
                    Directory.GetFiles(folderPath, "*.txt").Length
                );

                Console.WriteLine($"✅ Found {count} images in {folderPath}");
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting image count: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // METHOD 5: Load Images by Object Type
        // ============================================================
        [McpServerTool, Description("Load images filtered by object type (0-9). Useful for object-specific training.")]
        public async Task<int[][]> LoadImagesByObjectType(
            [Description("Path to folder containing images")] string folderPath,
            [Description("Object type to filter by (0-9 for digits)")] string objectType,
            [Description("How many images to load")] int numberOfImages)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

                int[][] imageData = await Task.Run(() =>
                {
                    var filePaths = Directory.GetFiles(folderPath, $"{objectType}_*.txt")
                        .Take(numberOfImages)
                        .ToArray();

                    return filePaths.Select(ImageLoader.LoadImage).ToArray();
                });

                Console.WriteLine($"✅ Loaded {imageData.Length} images of type '{objectType}'");
                return imageData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading filtered images: {ex.Message}");
                throw;
            }
        }
    }
}