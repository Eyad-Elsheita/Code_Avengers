using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageProcessing.Tests
{
    [TestClass]
    [DoNotParallelize] // Prevent tests from interfering with one another
    public class ImageProcessorTests
    {
        private string inputFolder = string.Empty;
        private string outputFolder = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            // Create unique folder names for each test run
            inputFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestInput_" + Guid.NewGuid().ToString());
            outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestOutput_" + Guid.NewGuid().ToString());

            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(outputFolder);

            // Set environment variables so that ImageProcessor picks these up
            Environment.SetEnvironmentVariable("Training_Image_Sample", inputFolder);
            Environment.SetEnvironmentVariable("Training_Image_Binary", outputFolder);

            Assert.IsTrue(Directory.Exists(inputFolder), "Input folder was not created.");
            Assert.IsTrue(Directory.Exists(outputFolder), "Output folder was not created.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldProcessValidImages()
        {
            // Arrange: Create a minimal valid PNG (1x1 pixel) using ImageSharp
            var testFilePath = Path.Combine(inputFolder, "3_001.png");
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Save(testFilePath, new PngEncoder());
            }

            Assert.IsTrue(File.Exists(testFilePath), "Test input file was not created.");

            // Act
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert: Verify the output file exists in the output folder
            var expectedOutputFile = Path.Combine(outputFolder, "3_001_binarized.txt");
            Assert.IsTrue(File.Exists(expectedOutputFile), "Output file was not created.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldHandleMissingEnvironmentVariables()
        {
            // Arrange: Clear the environment variables.
            Environment.SetEnvironmentVariable("Training_Image_Sample", null);
            Environment.SetEnvironmentVariable("Training_Image_Binary", null);

            // Act & Assert: Expect an exception when trying to retrieve null environment variables.
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary"));

            // Optionally, check that the exception message contains a helpful message.
            Assert.IsTrue(ex.Message.Contains("Value cannot be null"),
                "The exception message should indicate that a required environment variable is missing.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldHandleEmptyInputDirectory()
        {
            // Arrange: Input folder is already created and empty from Setup

            // Act
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert: No output files should be created
            Assert.AreEqual(0, Directory.GetFiles(outputFolder).Length,
                "No output files should be created for an empty input directory.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldSkipInvalidFileNames()
        {
            // Arrange: Create a minimal valid PNG with an invalid file name pattern using ImageSharp
            var invalidFilePath = Path.Combine(inputFolder, "invalidfile.png");
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Save(invalidFilePath, new PngEncoder());
            }

            // Act
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert: Verify that no output file was created for the invalid file name
            var unexpectedOutputFile = Path.Combine(outputFolder, "invalidfile_binarized.txt");
            Assert.IsFalse(File.Exists(unexpectedOutputFile), "Invalid file should not be processed.");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Force garbage collection to ensure all file handles are released
            GC.Collect();
            GC.WaitForPendingFinalizers();

            try
            {
                if (Directory.Exists(inputFolder))
                    Directory.Delete(inputFolder, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting input folder: {ex.Message}");
            }

            try
            {
                if (Directory.Exists(outputFolder))
                    Directory.Delete(outputFolder, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting output folder: {ex.Message}");
            }
        }
    }
}
