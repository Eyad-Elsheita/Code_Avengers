using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageProcessing.Tests
{
    /// <summary>
    /// Contains comprehensive tests for ImageProcessor's core functionality including
    /// image conversion, error handling, and environment configuration
    /// </summary>
    [TestClass]
    [DoNotParallelize] // Ensures sequential execution to prevent file system conflicts
    public class ImageProcessorTests
    {
        private string inputFolder = string.Empty;
        private string outputFolder = string.Empty;

        /// <summary>
        /// Initializes fresh test environment before each test execution
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Create unique GUID-based directories to prevent test interference
            inputFolder = Path.Combine(Directory.GetCurrentDirectory(),
                $"TestInput_{Guid.NewGuid()}");
            outputFolder = Path.Combine(Directory.GetCurrentDirectory(),
                $"TestOutput_{Guid.NewGuid()}");

            // Initialize directory structure
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(outputFolder);

            // Configure environment variables for the processor
            Environment.SetEnvironmentVariable("Training_Image_Sample", inputFolder);
            Environment.SetEnvironmentVariable("Training_Image_Binary", outputFolder);

            // Verify directory creation
            Assert.IsTrue(Directory.Exists(inputFolder), "Input test directory initialization failed");
            Assert.IsTrue(Directory.Exists(outputFolder), "Output test directory initialization failed");
        }

        /// <summary>
        /// Verifies successful conversion of valid PNG images to binary format
        /// </summary>
        [TestMethod]
        public void ConvertImagesToBinary_ShouldProcessValidImages()
        {
            // Arrange: Create minimal valid test image
            var testFilePath = Path.Combine(inputFolder, "3_001.png");
            using (var image = new Image<Rgba32>(1, 1)) // 1x1 pixel image
            {
                image.Save(testFilePath, new PngEncoder());
            }

            // Act: Execute core conversion functionality
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert: Validate output file creation and naming convention
            var expectedOutputFile = Path.Combine(outputFolder, "3_001_binarized.txt");
            Assert.IsTrue(File.Exists(expectedOutputFile),
                "Binarized output file was not generated");
        }

        /// <summary>
        /// Validates proper error handling for missing environment configuration
        /// </summary>
        [TestMethod]
        public void ConvertImagesToBinary_ShouldHandleMissingEnvironmentVariables()
        {
            // Arrange: Clear environment configuration
            Environment.SetEnvironmentVariable("Training_Image_Sample", null);
            Environment.SetEnvironmentVariable("Training_Image_Binary", null);

            // Act & Assert: Verify null argument handling
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary"));

            // Validate exception message content
            Assert.IsTrue(ex.Message.Contains("Value cannot be null"),
                "Exception message should clearly indicate missing configuration");
        }

        /// <summary>
        /// Ensures graceful handling of empty input directories
        /// </summary>
        [TestMethod]
        public void ConvertImagesToBinary_ShouldHandleEmptyInputDirectory()
        {
            // Act: Process empty directory
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert: Validate no phantom files created
            Assert.AreEqual(0, Directory.GetFiles(outputFolder).Length,
                "Empty input should result in zero output files");
        }

        /// <summary>
        /// Verifies proper filtering of files with non-compliant naming conventions
        /// </summary>
        [TestMethod]
        public void ConvertImagesToBinary_ShouldSkipInvalidFileNames()
        {
            // Arrange: Create test file with invalid naming pattern
            var invalidFilePath = Path.Combine(inputFolder, "invalidfile.png");
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Save(invalidFilePath, new PngEncoder());
            }

            // Act: Execute conversion process
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert: Verify invalid file was ignored
            var unexpectedOutputFile = Path.Combine(outputFolder, "invalidfile_binarized.txt");
            Assert.IsFalse(File.Exists(unexpectedOutputFile),
                "Non-compliant files should be skipped during processing");
        }

        /// <summary>
        /// Cleans up test artifacts and resets environment state
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            // Force resource release to handle file locks
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Remove test directories with error handling
            TryDeleteDirectory(inputFolder);
            TryDeleteDirectory(outputFolder);
        }

        /// <summary>
        /// Safely removes directories with exception handling
        /// </summary>
        private void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup error for {path}: {ex.Message}");
            }
        }
    }
}