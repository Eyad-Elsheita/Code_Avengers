using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ImageProcessing.Tests
{
    [TestClass]
    public class ImageLoaderTests
    {
        [TestMethod]
        public void LoadImageData_ShouldReturnValidData_WhenFilesExist()
        {
            // Arrange
            var folderPath = "TestFolder";
            Directory.CreateDirectory(folderPath);
            File.WriteAllText(Path.Combine(folderPath, "test1.txt"), "101\n010");
            File.WriteAllText(Path.Combine(folderPath, "test2.txt"), "110\n001");

            // Act
            var result = ImageLoader.LoadImageData(folderPath, 2);

            // Assert
            Assert.AreEqual(2, result.Length);
            CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1, 0 }, result[0]);
            CollectionAssert.AreEqual(new[] { 1, 1, 0, 0, 0, 1 }, result[1]);

            // Cleanup
            Directory.Delete(folderPath, true);
        }

        [TestMethod]
        public void LoadImage_ShouldHandleEmptyFileGracefully()
        {
            // Arrange
            var filePath = "empty.txt";
            File.WriteAllText(filePath, "");

            // Act
            var result = ImageLoader.LoadImage(filePath);

            // Assert
            Assert.AreEqual(0, result.Length);

            // Cleanup
            File.Delete(filePath);
        }

        [TestMethod]
        public void SaveImageDataToFile_ShouldWriteDataToFile()
        {
            // Arrange
            var filePath = "output.txt"; // Note: no directory specified
            var imageData = new[] { 1, 0, 1 };

            // Act
            ImageLoader.SaveImageDataToFile(imageData, filePath);

            // Assert
            Assert.IsTrue(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.AreEqual("1,0,1", content);

            // Cleanup
            File.Delete(filePath);
        }

    }
}
