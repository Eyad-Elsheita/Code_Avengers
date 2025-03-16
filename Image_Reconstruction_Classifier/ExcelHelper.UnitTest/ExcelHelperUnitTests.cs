using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExcelHelperTests
{
    [TestClass]
    public class ExcelHelperUnitTests
    {
        private string _testFilePath;

        [TestInitialize]
        public void Setup()
        {
            // Generate a temp file for testing
            _testFilePath = Path.Combine(Path.GetTempPath(), "TestSimilarityData.xlsx");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Delete the test file after each test to prevent conflicts
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [TestMethod]
        public void SaveSimilarityStatistics_Should_CreateExcelFile_WithCorrectData()
        {
            // Arrange: Create sample similarity data
            var testData = new List<SimilarityData>
            {
                new SimilarityData { PictureName = "Image1.png", VectorSimilarityPercentage = 85.4, BinarySimilarityPercentage = 90.2 },
                new SimilarityData { PictureName = "Image2.png", VectorSimilarityPercentage = 78.1, BinarySimilarityPercentage = 82.7 }
            };

            double expectedAvgVector = Math.Round(testData.Average(d => d.VectorSimilarityPercentage), 2);
            double expectedAvgBinary = Math.Round(testData.Average(d => d.BinarySimilarityPercentage), 2);

            // Act: Save to Excel
            ExcelHelper.SaveSimilarityStatistics(testData, _testFilePath);

            // Assert: Ensure file was created
            Assert.IsTrue(File.Exists(_testFilePath), "Excel file was not created.");

            // Load the saved Excel file and verify content
            using (var workbook = new XLWorkbook(_testFilePath))
            {
                var worksheet = workbook.Worksheet("Similarity Statistics");

                // Check headers
                Assert.AreEqual("Picture Name", worksheet.Cell(1, 1).GetString());
                Assert.AreEqual("Vector Similarity Percentage", worksheet.Cell(1, 2).GetString());
                Assert.AreEqual("Binary Similarity Percentage", worksheet.Cell(1, 3).GetString());
                Assert.AreEqual("Average Vector Similarity Percentage", worksheet.Cell(1, 4).GetString());
                Assert.AreEqual("Average Binary Similarity Percentage", worksheet.Cell(1, 5).GetString());

                // Check first row data
                Assert.AreEqual("Image1.png", worksheet.Cell(2, 1).GetString());
                Assert.AreEqual(85.4, worksheet.Cell(2, 2).GetDouble());
                Assert.AreEqual(90.2, worksheet.Cell(2, 3).GetDouble());

                // Check second row data
                Assert.AreEqual("Image2.png", worksheet.Cell(3, 1).GetString());
                Assert.AreEqual(78.1, worksheet.Cell(3, 2).GetDouble());
                Assert.AreEqual(82.7, worksheet.Cell(3, 3).GetDouble());

                // Check computed averages
                Assert.AreEqual(expectedAvgVector, worksheet.Cell(2, 4).GetDouble());
                Assert.AreEqual(expectedAvgBinary, worksheet.Cell(2, 5).GetDouble());
            }
        }
    }
}
