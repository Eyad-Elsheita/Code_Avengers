using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ImageProcessing.Tests
{
    [TestClass]
    public class ImageProcessorTests
    {
        private string inputFolder;
        private string outputFolder;

        [TestInitialize]
        public void Setup()
        {
            // Set up input and output folders
            inputFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestInput");
            outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestOutput");

            // Clean up any existing folders
            if (Directory.Exists(inputFolder)) Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);

            // Recreate the directories
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(outputFolder);

            // Verify directories exist
            Assert.IsTrue(Directory.Exists(inputFolder), "Input folder was not created.");
            Assert.IsTrue(Directory.Exists(outputFolder), "Output folder was not created.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldProcessValidImages()
        {
            // Arrange
            var testFilePath = Path.Combine(inputFolder, "3_001.png");
            File.WriteAllText(testFilePath, "TestImageContent"); // Simulated valid file

            // Verify test file exists
            Assert.IsTrue(File.Exists(testFilePath), "Test input file was not created.");

            // Act
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert
            var expectedOutputFile = Path.Combine(outputFolder, "3_001_binarized.txt");
            Assert.IsTrue(File.Exists(expectedOutputFile), "Output file was not created.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldHandleEmptyInputDirectory()
        {
            // Arrange
            Directory.CreateDirectory(inputFolder); // Ensure the input folder exists but is empty

            // Act
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert
            Assert.AreEqual(0, Directory.GetFiles(outputFolder).Length,
                "No output files should be created for an empty input directory.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldSkipInvalidFileNames()
        {
            // Arrange
            var invalidFilePath = Path.Combine(inputFolder, "invalidfile.png");
            File.WriteAllText(invalidFilePath, "TestImageContent"); // Simulated invalid file

            // Act
            ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");

            // Assert
            var unexpectedOutputFile = Path.Combine(outputFolder, "invalidfile_binarized.txt");
            Assert.IsFalse(File.Exists(unexpectedOutputFile), "Invalid file should not be processed.");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Safely clean up directories
            if (Directory.Exists(inputFolder)) Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);
        }
    }


}
