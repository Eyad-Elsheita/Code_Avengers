using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageReconstructionTests
{
    /// <summary>
    /// Contains unit tests for the ArrayExtensions class functionality
    /// </summary>
    [TestClass]
    public class ArrayExtensionsTests
    {
        /// <summary>
        /// Verifies correct conversion from 1D array to 2D array with specified dimensions
        /// </summary>
        [TestMethod]
        public void Reshape_Should_Convert_1D_To_2D_Correctly()
        {
            // Arrange - Create test data and expected results
            int[] inputArray = { 1, 2, 3, 4, 5, 6 };
            int width = 3, height = 2;  // 3 columns x 2 rows

            // Expected 2D array structure:
            // First row:  [1, 2, 3]
            // Second row: [4, 5, 6]
            int[,] expected = { { 1, 2, 3 }, { 4, 5, 6 } };

            // Act - Perform the reshape operation
            int[,] result = inputArray.Reshape(width, height);

            // Assert - Verify dimensional correctness and element positions
            // Check all elements match expected values
            for (int i = 0; i < height; i++)     // Row iteration
            {
                for (int j = 0; j < width; j++)  // Column iteration
                {
                    Assert.AreEqual(
                        expected[i, j],
                        result[i, j],
                        $"Element mismatch at position ({i},{j})"
                    );
                }
            }
        }

        /// <summary>
        /// Verifies proper error handling for invalid dimension combinations
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Reshape_Should_Throw_Exception_For_Invalid_Size()
        {
            // Arrange - Create incompatible dimensions
            int[] inputArray = { 1, 2, 3, 4, 5 };  // 5 elements
            int width = 3, height = 2;              // Requires 6 elements (3x2)

            // Act - This should throw ArgumentException
            inputArray.Reshape(width, height);

            // Assert - Handled by ExpectedException attribute
        }
    }
}