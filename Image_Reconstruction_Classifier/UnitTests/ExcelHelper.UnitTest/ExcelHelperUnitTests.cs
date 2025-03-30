using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExcelHelperTests
{
    /// <summary>
    /// Test class to validate the functionality of the <see cref="ExcelHelper"/> class.
    /// </summary>
    [TestClass]
    public class ExcelHelperUnitTests
    {
        /// <summary>
        /// Path to the temporary test file that will be used for saving the Excel file.
        /// </summary>
        private string _testFilePath = null!;

        /// <summary>
        /// Setup method that is executed before each test case to initialize necessary resources.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Generate a temporary file path to store the test Excel file for each test.
            _testFilePath = Path.Combine(Path.GetTempPath(), "TestSimilarityData.xlsx");
        }

        /// <summary>
        /// Cleanup method that is executed after each test case to clean up any resources used during the test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            // Ensure that the test file is deleted after each test to prevent conflicts with other tests
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        /// <summary>
        /// Test method that verifies if similarity statistics are saved correctly in an Excel file.
        /// It checks both the creation of the file and its content.
        /// </summary>
        [TestMethod]
        public void SaveSimilarityStatistics_Should_CreateExcelFile_WithCorrectData()
        {
            // Arrange: Create sample similarity data to simulate the data that will be saved in the Excel file
            var testData = new List<SimilarityData>
            {
                new SimilarityData { PictureName = "Image1.png", VectorSimilarityPercentage = 85.4, BinarySimilarityPercentage = 90.2 },
                new SimilarityData { PictureName = "Image2.png", VectorSimilarityPercentage = 78.1, BinarySimilarityPercentage = 82.7 }
            };

            // Calculate the expected average values for vector and binary similarity percentages
            double expectedAvgVector = Math.Round(testData.Average(d => d.VectorSimilarityPercentage), 2);
            double expectedAvgBinary = Math.Round(testData.Average(d => d.BinarySimilarityPercentage), 2);

            // Act: Save the sample similarity data to an Excel file using the SaveSimilarityStatistics method
            ExcelHelper.SaveSimilarityStatistics(testData, _testFilePath);

            // Assert: Check if the Excel file was created successfully
            Assert.IsTrue(File.Exists(_testFilePath), "Excel file was not created.");

            // Load the saved Excel file and verify its contents
            using (var workbook = new XLWorkbook(_testFilePath))
            {
                var worksheet = workbook.Worksheet("Similarity Statistics");

                // Check if the headers are correct
                Assert.AreEqual("Picture Name", worksheet.Cell(1, 1).GetString());
                Assert.AreEqual("Vector Similarity Percentage", worksheet.Cell(1, 2).GetString());
                Assert.AreEqual("Binary Similarity Percentage", worksheet.Cell(1, 3).GetString());
                Assert.AreEqual("Average Vector Similarity Percentage", worksheet.Cell(1, 4).GetString());
                Assert.AreEqual("Average Binary Similarity Percentage", worksheet.Cell(1, 5).GetString());

                // Check the first row of data
                Assert.AreEqual("Image1.png", worksheet.Cell(2, 1).GetString());
                Assert.AreEqual(85.4, worksheet.Cell(2, 2).GetDouble());
                Assert.AreEqual(90.2, worksheet.Cell(2, 3).GetDouble());

                // Check the second row of data
                Assert.AreEqual("Image2.png", worksheet.Cell(3, 1).GetString());
                Assert.AreEqual(78.1, worksheet.Cell(3, 2).GetDouble());
                Assert.AreEqual(82.7, worksheet.Cell(3, 3).GetDouble());

                // Check the computed averages for vector and binary similarity
                Assert.AreEqual(expectedAvgVector, worksheet.Cell(2, 4).GetDouble());
                Assert.AreEqual(expectedAvgBinary, worksheet.Cell(2, 5).GetDouble());
            }
        }
    }

    /// <summary>
    /// Class representing the similarity data for an image, including its name and similarity percentages.
    /// </summary>
    public class SimilarityData
    {
        /// <summary>
        /// Gets or sets the name of the picture/image.
        /// </summary>
        public string PictureName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the vector similarity percentage.
        /// </summary>
        public double VectorSimilarityPercentage { get; set; }

        /// <summary>
        /// Gets or sets the binary similarity percentage.
        /// </summary>
        public double BinarySimilarityPercentage { get; set; }
    }

    /// <summary>
    /// Helper class to manage Excel file operations related to similarity statistics.
    /// </summary>
    public static class ExcelHelper
    {
        /// <summary>
        /// Saves similarity statistics to an Excel file.
        /// </summary>
        /// <param name="similarityData">The list of similarity data to be saved.</param>
        /// <param name="filePath">The path to the Excel file where the data will be saved.</param>
        public static void SaveSimilarityStatistics(List<SimilarityData> similarityData, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                // Create a worksheet for the similarity statistics
                var worksheet = workbook.AddWorksheet("Similarity Statistics");

                // Add column headers
                worksheet.Cell(1, 1).Value = "Picture Name";
                worksheet.Cell(1, 2).Value = "Vector Similarity Percentage";
                worksheet.Cell(1, 3).Value = "Binary Similarity Percentage";
                worksheet.Cell(1, 4).Value = "Average Vector Similarity Percentage";
                worksheet.Cell(1, 5).Value = "Average Binary Similarity Percentage";

                // Write data rows for each image similarity
                for (int i = 0; i < similarityData.Count; i++)
                {
                    var data = similarityData[i];
                    worksheet.Cell(i + 2, 1).Value = data.PictureName;
                    worksheet.Cell(i + 2, 2).Value = data.VectorSimilarityPercentage;
                    worksheet.Cell(i + 2, 3).Value = data.BinarySimilarityPercentage;
                    worksheet.Cell(i + 2, 4).Value = similarityData.Average(d => d.VectorSimilarityPercentage);
                    worksheet.Cell(i + 2, 5).Value = similarityData.Average(d => d.BinarySimilarityPercentage);
                }

                // Save the workbook to the specified file path
                workbook.SaveAs(filePath);
            }
        }
    }
}
