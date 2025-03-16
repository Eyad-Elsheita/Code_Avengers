using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace ImageReconstructionTests
{
    [TestClass]
    public class ArrayExtensionsTests
    {
        [TestMethod]
        public void Reshape_Should_Convert_1D_To_2D_Correctly()
        {
            // Arrange
            int[] inputArray = { 1, 2, 3, 4, 5, 6 };
            int width = 3, height = 2;

            // Expected 2D array:
            // [ 1, 2, 3 ]
            // [ 4, 5, 6 ]
            int[,] expected = { { 1, 2, 3 }, { 4, 5, 6 } };

            // Act
            int[,] result = inputArray.Reshape(width, height);

            // Assert - Compare each element
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Assert.AreEqual(expected[i, j], result[i, j], $"Mismatch at ({i},{j})");
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Reshape_Should_Throw_Exception_For_Invalid_Size()
        {
            // Arrange
            int[] inputArray = { 1, 2, 3, 4, 5 }; // 5 elements
            int width = 3, height = 2; // Requires 6 elements

            // Act
            inputArray.Reshape(width, height); // Should throw exception
        }
    }

} 