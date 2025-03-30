using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Binary2Image.UnitTest
{
    /// <summary>
    /// Contains unit tests for the BinaryToImageConverter class functionality
    /// </summary>
    [TestClass]
    public class BinaryToImageConverterTests
    {
        /// <summary>
        /// Verifies successful PNG file creation with correct pixel values and dimensions
        /// </summary>
        [TestMethod]
        public void SaveBinaryAsPng_CreatesValidImageFile()
        {
            // Arrange: Create a 3x3 checkerboard pattern binary array
            int[] binaryImage = {
                1, 0, 1,  // Row 1: White, Black, White
                0, 1, 0,  // Row 2: Black, White, Black
                1, 0, 1   // Row 3: White, Black, White
            };
            const int width = 3, height = 3;
            string outputPath = "testImage.png";

            // Act: Execute the conversion method
            BinaryToImageConverter.SaveBinaryAsPng(binaryImage, width, height, outputPath);

            // Assert: Verify file creation and basic properties
            Assert.IsTrue(File.Exists(outputPath), "Output file was not created");

            // Detailed image validation using ImageSharp
            using (Image<Rgba32> image = Image.Load<Rgba32>(outputPath))
            {
                // Validate dimensions match specifications
                Assert.AreEqual(width, image.Width, "Image width mismatch");
                Assert.AreEqual(height, image.Height, "Image height mismatch");

                // Pixel-by-pixel validation against original binary array
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        var expectedColor = binaryImage[index] == 1
                            ? new Rgba32(255, 255, 255, 255) // White
                            : new Rgba32(0, 0, 0, 255);       // Black

                        Assert.AreEqual(expectedColor, image[x, y],
                            $"Pixel mismatch at position ({x},{y})");
                    }
                }
            }

            // Cleanup: Remove test artifact
            File.Delete(outputPath);
        }

        /// <summary>
        /// Validates proper error handling for dimension mismatch scenarios
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SaveBinaryAsPng_InvalidBinaryImageLength_ThrowsException()
        {
            // Arrange: Create invalid data (4 elements for 3x3=9 element expectation)
            int[] binaryImage = { 1, 0, 1, 0 }; // 4 elements
            const int width = 3, height = 3;     // Requires 9 elements
            string outputPath = "testImage.png";

            // Act & Assert: Verify exception propagation
            BinaryToImageConverter.SaveBinaryAsPng(binaryImage, width, height, outputPath);
        }

        /// <summary>
        /// Ensures null input validation is properly handled
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SaveBinaryAsPng_NullBinaryImage_ThrowsException()
        {
            // Arrange: Create null input scenario
            int[]? binaryImage = null;
            const int width = 3, height = 3;
            string outputPath = "testImage.png";

            // Act & Assert: Verify null argument handling
            BinaryToImageConverter.SaveBinaryAsPng(binaryImage!, width, height, outputPath);
        }

        /// <summary>
        /// Validates proper error handling for invalid filesystem paths
        /// </summary>
        [TestMethod]
        public void SaveBinaryAsPng_InvalidFilePath_ThrowsException()
        {
            // Arrange: Create invalid path scenario
            int[] binaryImage = {
                1, 0, 1,
                0, 1, 0,
                1, 0, 1
            };
            const int width = 3, height = 3;
            string invalidOutputPath = "invalid:/path/testImage.png"; // Invalid format

            // Act & Assert: Verify IO exception handling
            Assert.ThrowsException<IOException>(() =>
                BinaryToImageConverter.SaveBinaryAsPng(binaryImage, width, height, invalidOutputPath));
        }

        /// <summary>
        /// Verifies thread-safe operation during concurrent image generation
        /// </summary>
        [TestMethod]
        public async Task SaveBinaryAsPng_Concurrency_WorksCorrectly()
        {
            // Arrange: Create two distinct test patterns
            int[] binaryImage1 = {
                1, 0, 1,  // Pattern 1
                0, 1, 0,
                1, 0, 1
            };
            int[] binaryImage2 = {
                0, 1, 0,  // Pattern 2
                1, 0, 1,
                0, 1, 0
            };
            const int width = 3, height = 3;
            string outputPath1 = "testImage1.png";
            string outputPath2 = "testImage2.png";

            // Act: Execute concurrent conversions
            Task task1 = Task.Run(() =>
                BinaryToImageConverter.SaveBinaryAsPng(binaryImage1, width, height, outputPath1));
            Task task2 = Task.Run(() =>
                BinaryToImageConverter.SaveBinaryAsPng(binaryImage2, width, height, outputPath2));

            await Task.WhenAll(task1, task2);

            // Assert: Verify both files created successfully
            Assert.IsTrue(File.Exists(outputPath1), "First output file missing");
            Assert.IsTrue(File.Exists(outputPath2), "Second output file missing");

            // Cleanup: Remove test artifacts
            File.Delete(outputPath1);
            File.Delete(outputPath2);
        }
    }
}