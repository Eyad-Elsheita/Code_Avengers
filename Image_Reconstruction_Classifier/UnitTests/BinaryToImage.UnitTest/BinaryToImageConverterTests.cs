using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Binary2Image.UnitTest
{
    [TestClass]
    public class BinaryToImageConverterTests
    {
        // This test verifies that the SaveBinaryAsPng method correctly creates a PNG image file.
        [TestMethod]
        public void SaveBinaryAsPng_CreatesValidImageFile()
        {
            // Arrange: Define a 3x3 binary image and the expected output path.
            int[] binaryImage = new int[]
            {
                1, 0, 1,
                0, 1, 0,
                1, 0, 1
            };
            int width = 3;
            int height = 3;
            string outputPath = "testImage.png";

            // Act: Convert the binary image to a PNG file.
            BinaryToImageConverter.SaveBinaryAsPng(binaryImage, width, height, outputPath);

            // Assert: Check if the file was created.
            Assert.IsTrue(File.Exists(outputPath), "The PNG file was not created.");

            // Load the generated image to verify its content.
            using (Image<Rgba32> image = Image.Load<Rgba32>(outputPath))
            {
                // Verify that the image dimensions match the expected size.
                Assert.AreEqual(width, image.Width, "Image width is incorrect.");
                Assert.AreEqual(height, image.Height, "Image height is incorrect.");

                // Check if each pixel in the image has the correct color.
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        Rgba32 expectedColor = binaryImage[index] == 1
                            ? new Rgba32(255, 255, 255, 255)  // White for 1
                            : new Rgba32(0, 0, 0, 255);      // Black for 0

                        Assert.AreEqual(expectedColor, image[x, y], $"Pixel at ({x}, {y}) is incorrect.");
                    }
                }
            }

            // Cleanup: Delete the test image file after verification.
            File.Delete(outputPath);
        }

        // This test ensures that an exception is thrown when the binary image array length does not match width * height.
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SaveBinaryAsPng_InvalidBinaryImageLength_ThrowsException()
        {
            // Arrange: Define a binary image with incorrect dimensions.
            int[] binaryImage = new int[] { 1, 0, 1, 0 }; // Incorrect length
            int width = 3;
            int height = 3;
            string outputPath = "testImage.png";

            // Act: Attempt to create a PNG file, which should throw an exception.
            BinaryToImageConverter.SaveBinaryAsPng(binaryImage, width, height, outputPath);
        }

        // This test verifies that passing a null binary image throws an ArgumentNullException.
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SaveBinaryAsPng_NullBinaryImage_ThrowsException()
        {
            // Arrange: Define a null binary image.
            int[]? binaryImage = null;
            int width = 3;
            int height = 3;
            string outputPath = "testImage.png";

            // Act: Attempt to create a PNG file, which should throw an exception.
            BinaryToImageConverter.SaveBinaryAsPng(binaryImage!, width, height, outputPath);
        }

        // This test ensures that an invalid file path results in an IOException.
        [TestMethod]
        public void SaveBinaryAsPng_InvalidFilePath_ThrowsException()
        {
            // Arrange: Define a valid binary image but an invalid file path.
            int[] binaryImage = new int[]
            {
                1, 0, 1,
                0, 1, 0,
                1, 0, 1
            };
            int width = 3;
            int height = 3;
            string invalidOutputPath = "invalid:/path/testImage.png"; // Invalid path

            // Act & Assert: Ensure that attempting to save to an invalid path throws an IOException.
            Assert.ThrowsException<IOException>(() =>
                BinaryToImageConverter.SaveBinaryAsPng(binaryImage, width, height, invalidOutputPath));
        }

        // This test verifies that the method works correctly when executed concurrently.
        [TestMethod]
        public async Task SaveBinaryAsPng_Concurrency_WorksCorrectly()
        {
            // Arrange: Define two different binary images.
            int[] binaryImage1 = new int[]
            {
                1, 0, 1,
                0, 1, 0,
                1, 0, 1
            };
            int[] binaryImage2 = new int[]
            {
                0, 1, 0,
                1, 0, 1,
                0, 1, 0
            };
            int width = 3;
            int height = 3;
            string outputPath1 = "testImage1.png";
            string outputPath2 = "testImage2.png";

            // Act: Run two SaveBinaryAsPng calls concurrently.
            Task task1 = Task.Run(() => BinaryToImageConverter.SaveBinaryAsPng(binaryImage1, width, height, outputPath1));
            Task task2 = Task.Run(() => BinaryToImageConverter.SaveBinaryAsPng(binaryImage2, width, height, outputPath2));
            await Task.WhenAll(task1, task2); // Wait for both tasks to complete.

            // Assert: Ensure that both image files were successfully created.
            Assert.IsTrue(File.Exists(outputPath1), "First PNG file was not created.");
            Assert.IsTrue(File.Exists(outputPath2), "Second PNG file was not created.");

            // Cleanup: Delete the test images after verification.
            File.Delete(outputPath1);
            File.Delete(outputPath2);
        }
    }
}