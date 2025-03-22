using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Image_Reconstruction_Classifier;

namespace ImageReconstructionTests
{
    [TestClass]
    public class ImageSpatialTests
    {
        private const string TestImageLoader = "Test_Image_Loader";
        private const string TestImageSpatial = "Test_Image_Spatial";

        // === Helper Method to Clean and Setup Directories ===
        private void PrepareTestFolders(string inputFolder, string outputFolder)
        {
            if (Directory.Exists(inputFolder)) Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);

            Directory.CreateDirectory(inputFolder);  // Ensure the input folder is created
            Directory.CreateDirectory(outputFolder); // Ensure the output folder is created

            // Log folder creation status
            Console.WriteLine($"Input folder exists: {Directory.Exists(inputFolder)}");
            Console.WriteLine($"Output folder exists: {Directory.Exists(outputFolder)}");
        }

        [TestMethod]
        public void Test_SaveImagesinSpartialPooler_WithValidEnvironmentVariables()
        {
            // Arrange
            var inputFolder = @"C:\MockInput";
            var outputFolder = @"C:\MockOutput";

            PrepareTestFolders(inputFolder, outputFolder);

            Environment.SetEnvironmentVariable("Training_Image_Loader", inputFolder);
            Environment.SetEnvironmentVariable("Training_Image_Spatial", outputFolder);

            // Create mock input file
            var mockFile = Path.Combine(inputFolder, "mock_encoder_output.txt");
            File.WriteAllText(mockFile, string.Join(",", Enumerable.Repeat(1, 784))); // Mock 28x28 image

            // Act
            ImageSpatial.SaveImagesinSpartialPooler();

            // Assert
            var outputFiles = Directory.GetFiles(outputFolder, "*_spatial.txt");
            Assert.AreEqual(1, outputFiles.Length, "Spatial output file should be created.");
            Assert.IsTrue(File.Exists(outputFiles[0]), "Spatial file is missing.");

            // Cleanup
            PrepareTestFolders(inputFolder, outputFolder);
        }

        [TestMethod]
        public void Test_ProcessTestImagesSpatial_EmptyFilesAreSkipped()
        {
            // Arrange
            var inputFolder = @"C:\MockTestInput_Empty";
            var outputFolder = @"C:\MockTestOutput_Empty";

            PrepareTestFolders(inputFolder, outputFolder);

            Environment.SetEnvironmentVariable("Test_Image_Loader", inputFolder);
            Environment.SetEnvironmentVariable("Test_Image_Spatial", outputFolder);

            // Create only an empty file
            var emptyFile = Path.Combine(inputFolder, "empty_test_file.txt");
            File.WriteAllText(emptyFile, ""); // Write an empty file

            // Act
            ImageSpatial.ProcessTestImagesSpatial();

            // Assert
            var outputFiles = Directory.GetFiles(outputFolder, "*_spatial.txt");
            Assert.AreEqual(0, outputFiles.Length, "No output files should be created for empty input.");

            // Cleanup
            PrepareTestFolders(inputFolder, outputFolder);
        }

        [TestMethod]
        public void Test_ProcessTestImagesSpatial_ValidFilesAreProcessed()
        {
            // Arrange
            var inputFolder = @"C:\MockTestInput_Valid";
            var outputFolder = @"C:\MockTestOutput_Valid";

            // Ensure directories are cleaned and prepared
            PrepareTestFolders(inputFolder, outputFolder);

            Environment.SetEnvironmentVariable(TestImageLoader, inputFolder);
            Environment.SetEnvironmentVariable(TestImageSpatial, outputFolder);

            // ===== Create Mock Input File Containing 784 Values =====
            var validFile = Path.Combine(inputFolder, "valid_test_file.txt");
            string imageData = string.Join(",", Enumerable.Repeat(1, 784)); // Mocking a 28x28 image
            File.WriteAllText(validFile, imageData); // Create the mock file
            // =====================================================

            // Ensure the file is created
            Console.WriteLine($"File created at: {validFile}");
            Assert.IsTrue(File.Exists(validFile), "Test file was not created!");

            // Act
            ImageSpatial.ProcessTestImagesSpatial();

            // Log the output folder contents before the assertion
            var outputFiles = Directory.GetFiles(outputFolder, "*_spatial.txt");

            // Log output files found
            Console.WriteLine($"Found {outputFiles.Length} spatial files in the output directory.");

            // Assert: Ensure exactly 1 spatial file exists
            Assert.AreEqual(1, outputFiles.Length, "Valid input should produce one spatial file.");
            Assert.IsTrue(File.Exists(outputFiles[0]), "Spatial file is missing.");

            // Cleanup
            PrepareTestFolders(inputFolder, outputFolder);
        }
    }
}
