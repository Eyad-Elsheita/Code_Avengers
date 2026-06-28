/// <summary>
/// Provides extension methods for array manipulation operations
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Converts a 1-dimensional array into a 2-dimensional array with specified dimensions
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <param name="array">The source 1D array to reshape</param>
    /// <param name="width">Number of columns in the output 2D array</param>
    /// <param name="height">Number of rows in the output 2D array</param>
    /// <returns>A 2D array with dimensions [height, width] containing the original elements</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the total elements in the 1D array don't match width * height
    /// </exception>
    /// <remarks>
    /// The reshaping operation follows row-major order: elements are filled row by row,
    /// starting from the first row (index 0) to the last row (index height-1)
    /// </remarks>
    public static T[,] Reshape<T>(this T[] array, int width, int height)
    {
        // Validate that the 1D array can be perfectly divided into the specified dimensions
        if (array.Length != width * height)
            throw new ArgumentException(
                $"Array size ({array.Length}) does not match " +
                $"the specified dimensions ({width}x{height} = {width * height}).");

        // Create a 2D array with dimensions [rows, columns] = [height, width]
        var result = new T[height, width];

        // Populate the 2D array using row-major order layout:
        // - Outer loop iterates through rows (vertical dimension)
        // - Inner loop iterates through columns (horizontal dimension)
        for (int i = 0; i < height; i++)         // Current row index
        {
            for (int j = 0; j < width; j++)      // Current column index
            {
                // Calculate the 1D index using the formula: (row * width) + column
                // This maps the 2D position to the original 1D array index
                result[i, j] = array[i * width + j];
            }
        }

        return result;
    }
}