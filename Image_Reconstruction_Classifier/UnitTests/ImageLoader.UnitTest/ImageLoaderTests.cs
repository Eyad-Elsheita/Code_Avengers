using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ImageProcessing.Tests
{
    /// <summary>
    /// Contains unit tests for verifying image data loading and saving functionality
    /// </summary>
    [TestClass]
    public class ImageLoaderTests
    {
        /// <summary>
        /// Verifies successful loading of multiple image files with valid binary data
        /// </summary>
        [TestMethod]
        public void LoadImageData_ShouldReturnValidData_WhenFilesExist()
        {
            // Arrange: Create test directory with sample binary files
            var folderPath = "TestFolder";
            Directory.CreateDirectory(folderPath);

            // Create test files with different binary patterns
            File.WriteAllText(Path.Combine(folderPath, "test1.txt"), "101\n010"); // 2x3 grid
            File.WriteAllText(Path.Combine(folderPath, "test2.txt"), "110\n001"); // 2x3 grid

            // Act: Load image data with specified maximum files
            var result = ImageLoader.LoadImageData(folderPath, 2);

            // Assert: Validate quantity and content of loaded images
            Assert.AreEqual(2, result.Length, "Incorrect number of loaded images");
            CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1, 0 }, result[0],
                "First image data mismatch");
            CollectionAssert.AreEqual(new[] { 1, 1, 0, 0, 0, 1 }, result[1],
                "Second image data mismatch");

            // Cleanup: Remove test artifacts
            Directory.Delete(folderPath, true);
        }

        /// <summary>
        /// Validates proper handling of empty input files
        /// </summary>
        [TestMethod]
        public void LoadImage_ShouldHandleEmptyFileGracefully()
        {
            // Arrange: Create empty test file
            var filePath = "empty.txt";
            File.WriteAllText(filePath, "");  // Zero-length file

            // Act: Attempt to load empty file
            var result = ImageLoader.LoadImage(filePath);

            // Assert: Verify empty array return
            Assert.AreEqual(0, result.Length,
                "Empty file should return zero-length array");

            // Cleanup: Remove test file
            File.Delete(filePath);
        }

        /// <summary>
        /// Verifies correct serialization and saving of image data
        /// </summary>
        [TestMethod]
        public void SaveImageDataToFile_ShouldWriteDataToFile()
        {
            // Arrange: Create test data and output path
            var filePath = "output.txt";  // Current directory output
            var imageData = new[] { 1, 0, 1 };  // Simple binary pattern

            // Act: Execute save operation
            ImageLoader.SaveImageDataToFile(imageData, filePath);

            // Assert: Verify file creation and content
            Assert.IsTrue(File.Exists(filePath), "Output file was not created");
            var content = File.ReadAllText(filePath);
            Assert.AreEqual("1,0,1", content, "File content mismatch");

            // Cleanup: Remove test file
            File.Delete(filePath);
        }
    }
}