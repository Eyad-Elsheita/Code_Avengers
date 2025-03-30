using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ImageProcessing.Tests
{
    /// <summary>
    /// Contains unit tests for verifying median filter functionality in image processing
    /// </summary>
    [TestClass]
    public class ImageFilterUnitTests
    {
        /// <summary>
        /// Verifies median filter operation on a standard 3x3 grayscale image matrix
        /// </summary>
        [TestMethod]
        public void ApplyMedianFilter_Should_CorrectlyFilter_3x3Image()
        {
            // Arrange: Create 3x3 test matrix with linear intensity progression
            // Original matrix:
            // [10, 20, 30]
            // [40, 50, 60]
            // [70, 80, 90]
            int[] image = { 10, 20, 30,
                            40, 50, 60,
                            70, 80, 90 };
            const int width = 3, height = 3;

            // Act: Apply median filter with 3x3 kernel
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Verify center pixel median calculation
            // Center pixel neighborhood (50) values: 
            // [10,20,30,40,50,60,70,80,90] → Median = 50
            Assert.AreEqual(50, filtered[4], "Center pixel median miscalculation");
        }

        /// <summary>
        /// Validates edge case handling for minimum viable image size (1x1 pixel)
        /// </summary>
        [TestMethod]
        public void ApplyMedianFilter_Should_Handle_1x1Image()
        {
            // Arrange: Single pixel test case
            int[] image = { 128 };
            const int width = 1, height = 1;

            // Act: Process single-pixel image
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Verify identity transformation
            // No neighborhood exists - original value should be preserved
            CollectionAssert.AreEqual(image, filtered,
                "Single-pixel image should remain unchanged");
        }

        /// <summary>
        /// Tests median filter operation on larger 5x5 matrix with sequential values
        /// </summary>
        [TestMethod]
        public void ApplyMedianFilter_Should_CorrectlyFilter_5x5Image()
        {
            // Arrange: Create 5x5 matrix with linear value progression
            // Matrix values range from 1-25 in row-major order
            int[] image = {
                1,  2,  3,  4,  5,
                6,  7,  8,  9,  10,
                11, 12, 13, 14, 15,
                16, 17, 18, 19, 20,
                21, 22, 23, 24, 25
            };
            const int width = 5, height = 5;

            // Act: Apply filter with 3x3 kernel
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Verify center pixel (13) remains median
            // Center neighborhood (13 at position 12) values:
            // [7,8,9,12,13,14,17,18,19] → Median = 13
            Assert.AreEqual(13, filtered[12],
                "Center pixel in ordered 5x5 matrix should maintain median value");
        }

        /// <summary>
        /// Validates filter stability on uniform intensity images
        /// </summary>
        [TestMethod]
        public void ApplyMedianFilter_Should_NotChange_AlreadyFilteredImage()
        {
            // Arrange: Create uniform 3x3 image matrix
            // All pixels have identical intensity value (100)
            int[] image = Enumerable.Repeat(100, 9).ToArray();
            const int width = 3, height = 3;

            // Act: Process uniform image
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Verify output matches input
            // All neighborhoods contain identical values - no changes expected
            CollectionAssert.AreEqual(image, filtered,
                "Uniform image should remain unchanged after median filtering");
        }
    }
}