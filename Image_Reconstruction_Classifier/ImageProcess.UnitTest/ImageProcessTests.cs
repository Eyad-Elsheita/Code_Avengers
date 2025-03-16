using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ImageProcessing.Tests
{
    [TestClass]
    public class ImageProcessTests
    {
        private string inputFolder;
        private string outputFolder;

        [TestInitialize]
        public void Setup()
        {
            // Set up input and output folders
            inputFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestInput");
            outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestOutput");

            // Set environment variables
            Environment.SetEnvironmentVariable("Training_Image_Sample", inputFolder);
            Environment.SetEnvironmentVariable("Training_Image_Binary", outputFolder);

            // Ensure folders are clean
            if (Directory.Exists(inputFolder)) Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);

            // Recreate directories
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(outputFolder);
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldProcessValidImages()
        {
            // Arrange
            // Create a test image file (simulating the file name format "3_001.png")
            var testFilePath = Path.Combine(inputFolder, "3_001.png");
            File.WriteAllText(testFilePath, "TestImageContent"); // Placeholder for actual image content

            // Act
            ImageProcessor.ConvertImagesToBinary();

            // Assert
            var expectedOutputFile = Path.Combine(outputFolder, "3_001_binarized.txt");
            Assert.IsTrue(File.Exists(expectedOutputFile), "Output file was not created.");

            // (Optional) Check content if the Binarizer generates specific output
            var fileContent = File.ReadAllText(expectedOutputFile);
            Assert.IsFalse(string.IsNullOrWhiteSpace(fileContent), "Output file is empty.");
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldSkipInvalidFileNames()
        {
            // Arrange
            // Create an invalid file name (e.g., missing underscore)
            var invalidFilePath = Path.Combine(inputFolder, "invalidfile.png");
            File.WriteAllText(invalidFilePath, "TestImageContent");

            // Act
            ImageProcessor.ConvertImagesToBinary();

            // Assert
            // Ensure no output file is created for the invalid file
            var unexpectedOutputFile = Path.Combine(outputFolder, "invalidfile_binarized.txt");
            Assert.IsFalse(File.Exists(unexpectedOutputFile), "Invalid file should not be processed.");
        }


        [TestMethod]
        public void ConvertImagesToBinary_ShouldHandleMissingEnvironmentVariables()
        {
            // Arrange
            Environment.SetEnvironmentVariable("Training_Image_Sample", null);
            Environment.SetEnvironmentVariable("Training_Image_Binary", null);

            // Act & Assert
            try
            {
                ImageProcessor.ConvertImagesToBinary();
                Assert.Fail("Expected an exception due to missing environment variables, but none was thrown.");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Value cannot be null"),
                    "The exception message should indicate missing environment variables.");
            }
        }

        [TestMethod]
        public void ConvertImagesToBinary_ShouldHandleEmptyInputDirectory()
        {
            // Arrange
            // Ensure input folder exists but is empty
            Directory.CreateDirectory(inputFolder);

            // Act
            ImageProcessor.ConvertImagesToBinary();

            // Assert
            Assert.AreEqual(0, Directory.GetFiles(outputFolder).Length,
                "No output files should be created for an empty input directory.");
        }



        [TestCleanup]
        public void Cleanup()
        {
            // Clean up directories and reset environment variables
            if (Directory.Exists(inputFolder)) Directory.Delete(inputFolder, true);
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);

            Environment.SetEnvironmentVariable("Training_Image_Sample", null);
            Environment.SetEnvironmentVariable("Training_Image_Binary", null);
        }
    }
}
