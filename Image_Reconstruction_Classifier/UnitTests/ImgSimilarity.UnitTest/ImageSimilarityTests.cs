using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ImageSimilarityTests
{
    /// <summary>
    /// Contains comprehensive unit tests for image similarity calculation and binary matrix conversion functionality
    /// </summary>
    [TestClass]
    public class ImageSimilarityUnitTests
    {
        /// <summary>
        /// Verifies cosine similarity calculation across various image comparison scenarios
        /// </summary>
        [DataTestMethod]
        // Partial match (2/4 pixels match) → 0.5 similarity
        [DataRow(new int[] { 1, 0, 1, 0 }, new int[] { 1, 1, 0, 0 }, 0.5)]
        // Perfect match → 1.0 similarity
        [DataRow(new int[] { 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1 }, 1.0)]
        // Complete mismatch → 0.0 similarity
        [DataRow(new int[] { 1, 1, 1, 1 }, new int[] { 0, 0, 0, 0 }, 0.0)]
        // Inverse patterns → 0.0 similarity
        [DataRow(new int[] { 1, 0, 0, 1 }, new int[] { 0, 1, 1, 0 }, 0.0)]
        public void CalculateCosineSimilarity_Should_ReturnCorrectValue(int[] image1, int[] image2, double expected)
        {
            // Act: Calculate similarity between test patterns
            double result = ImageSimilarity.CalculateCosineSimilarity(image1, image2);

            // Assert: Verify result with tolerance for floating-point precision
            Assert.AreEqual(expected, result, 0.0001,
                $"Cosine similarity calculation mismatch for test vectors");
        }

        /// <summary>
        /// Validates input validation for mismatched array lengths
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CalculateCosineSimilarity_Should_ThrowException_WhenArraysHaveDifferentLengths()
        {
            // Arrange: Create incompatible vectors (3 vs 4 elements)
            int[] image1 = { 1, 0, 1 };       // 3-element vector
            int[] image2 = { 1, 0, 1, 0 };    // 4-element vector

            // Act & Assert: Verify argument validation
            ImageSimilarity.CalculateCosineSimilarity(image1, image2);
        }

        /// <summary>
        /// Verifies correct binary matrix formatting for various image dimensions
        /// </summary>
        [TestMethod]
        // 2x2 matrix formatting
        [DataRow(new int[] { 1, 0, 1, 0 }, 2, "10\n10")]
        // 3x3 matrix with pattern variation
        [DataRow(new int[] { 1, 0, 0, 1, 1, 0, 0, 1, 1 }, 3, "100\n110\n011")]
        // Uniform 2x2 matrix
        [DataRow(new int[] { 1, 1, 1, 1 }, 2, "11\n11")]
        public void ConvertToBinaryMatrix_Should_ReturnCorrectFormat(int[] image, int rowSize, string expected)
        {
            // Act: Convert binary array to matrix string
            string result = ImageSimilarity.ConvertToBinaryMatrix(image, rowSize);

            // Assert: Verify exact string match including newline formatting
            Assert.AreEqual(expected, result,
                $"Binary matrix formatting failed for {rowSize}x{rowSize} case");
        }

        /// <summary>
        /// Validates error handling for invalid matrix dimensions
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConvertToBinaryMatrix_Should_ThrowException_WhenSizeMismatch()
        {
            // Arrange: Create invalid 5-element array with 3x3 expectation
            int[] image = { 1, 0, 1, 0, 1 };  // 5 elements
            int rowSize = 3;                    // Requires 9 elements (3x3)

            // Act & Assert: Verify dimension validation
            ImageSimilarity.ConvertToBinaryMatrix(image, rowSize);
        }
    }
}