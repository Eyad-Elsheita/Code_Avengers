using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ImageProcessing.Tests
{
    [TestClass]
    public class ImageFilterUnitTests
    {
        [TestMethod]
        public void ApplyMedianFilter_Should_CorrectlyFilter_3x3Image()
        {
            // Arrange: Simple 3x3 grayscale image
            int[] image = { 10, 20, 30,
                            40, 50, 60,
                            70, 80, 90 };
            int width = 3, height = 3;

            // Act
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Check median values in the center
            Assert.AreEqual(50, filtered[4]); // Center pixel should be the median
        }

        [TestMethod]
        public void ApplyMedianFilter_Should_Handle_1x1Image()
        {
            // Arrange: Single pixel image
            int[] image = { 128 };
            int width = 1, height = 1;

            // Act
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Should return the same image
            CollectionAssert.AreEqual(image, filtered);
        }

        [TestMethod]
        public void ApplyMedianFilter_Should_CorrectlyFilter_5x5Image()
        {
            // Arrange: A larger 5x5 image
            int[] image = {
                1,  2,  3,  4,  5,
                6,  7,  8,  9,  10,
                11, 12, 13, 14, 15,
                16, 17, 18, 19, 20,
                21, 22, 23, 24, 25
            };
            int width = 5, height = 5;

            // Act
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Center pixel (13) should remain median
            Assert.AreEqual(13, filtered[12]); // Center of 5x5 (row 3, col 3)
        }

        [TestMethod]
        public void ApplyMedianFilter_Should_NotChange_AlreadyFilteredImage()
        {
            // Arrange: Image where all values are the same
            int[] image = Enumerable.Repeat(100, 9).ToArray();
            int width = 3, height = 3;

            // Act
            int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

            // Assert: Filtered image should be unchanged
            CollectionAssert.AreEqual(image, filtered);
        }
    }
}
