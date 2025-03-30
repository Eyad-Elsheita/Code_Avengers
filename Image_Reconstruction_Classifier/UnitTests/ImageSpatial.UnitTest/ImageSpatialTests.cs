using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Image_Reconstruction_Classifier;

namespace ImageReconstructionTests
{
    /// <summary>
    /// Comprehensive unit tests for validating Spatial Pooler functionality
    /// in image reconstruction pipelines.
    /// </summary>
    /// <remarks>
    /// Test Coverage Includes:
    /// - Training pipeline validation
    /// - Inference pipeline validation
    /// - Edge case handling (empty files, invalid data)
    /// - Environment configuration resilience
    /// 
    /// Test Isolation:
    /// - Uses dedicated test directories
    /// - Automatic cleanup after each test
    /// - Independent environment configuration
    /// </remarks>
    [TestClass]
    public class ImageSpatialTests
    {
        // Environment configuration constants
        private const string TestImageLoader = "Test_Image_Loader";
        private const string TestImageSpatial = "Test_Image_Spatial";

        /// <summary>
        /// Prepares clean test directories and validates their creation
        /// </summary>
        /// <param name="inputFolder">Input directory path</param>
        /// <param name="outputFolder">Output directory path</param>
        private void PrepareTestFolders(string inputFolder, string outputFolder)
        {
            // Clean existing directories
            if (Directory.Exists(inputFolder)) Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);

            // Create fresh directories
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(outputFolder);

            // Diagnostic output
            Console.WriteLine($"[Setup] Input folder status: {Directory.Exists(inputFolder)}");
            Console.WriteLine($"[Setup] Output folder status: {Directory.Exists(outputFolder)}");
        }

        /// <summary>
        /// Validates training pipeline execution with properly configured environment
        /// and valid input data
        /// </summary>
        [TestMethod]
        public void Test_SaveImagesinSpartialPooler_WithValidEnvironmentVariables()
        {
            // Arrange
            var inputFolder = @"C:\MockInput";
            var outputFolder = @"C:\MockOutput";
            PrepareTestFolders(inputFolder, outputFolder);

            // Configure environment
            Environment.SetEnvironmentVariable("Training_Image_Loader", inputFolder);
            Environment.SetEnvironmentVariable("Training_Image_Spatial", outputFolder);

            // Create valid test input (784-element vector)
            var mockFile = Path.Combine(inputFolder, "mock_encoder_output.txt");
            File.WriteAllText(mockFile, string.Join(",", Enumerable.Repeat(1, 784)));

            // Act
            ImageSpatial.SaveImagesinSpartialPooler();

            // Assert
            var outputFiles = Directory.GetFiles(outputFolder, "*_spatial.txt");
            Assert.AreEqual(1, outputFiles.Length, "Incorrect number of output files");
            Assert.IsTrue(File.Exists(outputFiles[0]), "Output file missing");

            // Cleanup
            PrepareTestFolders(inputFolder, outputFolder);
        }

        /// <summary>
        /// Validates proper handling of empty input files during inference processing
        /// </summary>
        [TestMethod]
        public void Test_ProcessTestImagesSpatial_EmptyFilesAreSkipped()
        {
            // Arrange
            var inputFolder = @"C:\MockTestInput_Empty";
            var outputFolder = @"C:\MockTestOutput_Empty";
            PrepareTestFolders(inputFolder, outputFolder);

            // Set test-specific environment
            Environment.SetEnvironmentVariable("Test_Image_Loader", inputFolder);
            Environment.SetEnvironmentVariable("Test_Image_Spatial", outputFolder);

            // Create empty test file
            var emptyFile = Path.Combine(inputFolder, "empty_test_file.txt");
            File.WriteAllText(emptyFile, string.Empty);

            // Act
            ImageSpatial.ProcessTestImagesSpatial();

            // Assert
            var outputFiles = Directory.GetFiles(outputFolder, "*_spatial.txt");
            Assert.AreEqual(0, outputFiles.Length, "Empty input should produce no output");

            // Cleanup
            PrepareTestFolders(inputFolder, outputFolder);
        }

        /// <summary>
        /// Validates complete processing pipeline for valid test images,
        /// including input validation, spatial processing, and output generation
        /// </summary>
        [TestMethod]
        public void Test_ProcessTestImagesSpatial_ValidFilesAreProcessed()
        {
            // Arrange
            var inputFolder = @"C:\MockTestInput_Valid";
            var outputFolder = @"C:\MockTestOutput_Valid";
            PrepareTestFolders(inputFolder, outputFolder);

            // Configure test environment
            Environment.SetEnvironmentVariable(TestImageLoader, inputFolder);
            Environment.SetEnvironmentVariable(TestImageSpatial, outputFolder);

            // Create valid test data (784-element MNIST-style input)
            var validFile = Path.Combine(inputFolder, "valid_test_file.txt");
            File.WriteAllText(validFile, string.Join(",", Enumerable.Repeat(1, 784)));

            // Validate test setup
            Assert.IsTrue(File.Exists(validFile), "Test input creation failed");

            // Act
            ImageSpatial.ProcessTestImagesSpatial();

            // Assert
            var outputFiles = Directory.GetFiles(outputFolder, "*_spatial.txt");
            Console.WriteLine($"[Diagnostic] Found {outputFiles.Length} output files");
            Assert.AreEqual(1, outputFiles.Length, "Incorrect output count");
            Assert.IsTrue(File.Exists(outputFiles[0]), "Output file verification failed");

            // Cleanup
            PrepareTestFolders(inputFolder, outputFolder);
        }
    }
}