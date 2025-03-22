using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ImageSimilarityTests
{
    [TestClass]
    public class ImageSimilarityUnitTests
    {
        [DataTestMethod]
        [DataRow(new int[] { 1, 0, 1, 0 }, new int[] { 1, 1, 0, 0 }, 0.5)]
        [DataRow(new int[] { 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1 }, 1.0)] // Identical images → similarity = 1
        [DataRow(new int[] { 1, 1, 1, 1 }, new int[] { 0, 0, 0, 0 }, 0.0)] // One image is all zeros → similarity = 0
        [DataRow(new int[] { 1, 0, 0, 1 }, new int[] { 0, 1, 1, 0 }, 0.0)] // Completely different → similarity = 0
        public void CalculateCosineSimilarity_Should_ReturnCorrectValue(int[] image1, int[] image2, double expected)
        {
            // Act
            double result = ImageSimilarity.CalculateCosineSimilarity(image1, image2);

            // Assert (allowing small floating-point error tolerance)
            Assert.AreEqual(expected, result, 0.0001);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CalculateCosineSimilarity_Should_ThrowException_WhenArraysHaveDifferentLengths()
        {
            // Arrange: Different-sized arrays
            int[] image1 = { 1, 0, 1 };
            int[] image2 = { 1, 0, 1, 0 };

            // Act & Assert: Expect an exception
            ImageSimilarity.CalculateCosineSimilarity(image1, image2);
        }

        [TestMethod]
        [DataRow(new int[] { 1, 0, 1, 0 }, 2, "10\n10")] //  Square 2x2
        [DataRow(new int[] { 1, 0, 0, 1, 1, 0, 0, 1, 1 }, 3, "100\n110\n011")] //  Square 3x3
        [DataRow(new int[] { 1, 1, 1, 1 }, 2, "11\n11")] //  Square 2x2
        public void ConvertToBinaryMatrix_Should_ReturnCorrectFormat(int[] image, int rowSize, string expected)
        {
            // Act
            string result = ImageSimilarity.ConvertToBinaryMatrix(image, rowSize);

            // Assert
            Assert.AreEqual(expected, result);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConvertToBinaryMatrix_Should_ThrowException_WhenSizeMismatch()
        {
            // Arrange: Image array not forming a perfect square
            int[] image = { 1, 0, 1, 0, 1 };
            int rowSize = 3; // Should be 3x3, but we only have 5 elements

            // Act & Assert: Expect an exception
            ImageSimilarity.ConvertToBinaryMatrix(image, rowSize);
        }
    }
}
