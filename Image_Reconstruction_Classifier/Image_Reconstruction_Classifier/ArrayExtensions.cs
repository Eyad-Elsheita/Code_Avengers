public static class ArrayExtensions
{
    public static T[,] Reshape<T>(this T[] array, int width, int height)
    {
        if (array.Length != width * height)
            throw new ArgumentException("Array size does not match the specified dimensions.");

        var result = new T[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                result[i, j] = array[i * height + j];
            }
        }

        return result;
    }
}
