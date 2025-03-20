public static class ArrayExtensions
{
    public static T[,] Reshape<T>(this T[] array, int width, int height)
    {
        if (array.Length != width * height)
            throw new ArgumentException("Array size does not match the specified dimensions.");

        var result = new T[height, width]; // Adjusted dimensions

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                result[i, j] = array[i * width + j]; 
            }
        }

        return result;
    }

}