public class ImageFilter
{
    /// <summary>
    /// Applies a median filter to a binary image to reduce noise.
    /// The median filter replaces each pixel with the median value of its 3x3 neighborhood.
    /// </summary>
    /// <param name="image">A one-dimensional array representing the binary image (0s and 1s).</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <returns>A new array representing the filtered binary image.</returns>
    public static int[] ApplyMedianFilter(int[] image, int width, int height)
    {
        int[] filtered = new int[image.Length]; // Array to store the filtered image.

        // Iterate over each pixel in the image.
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                List<int> neighbors = new List<int>(); // List to store neighboring pixel values.

                // Gather a 3x3 neighborhood (handling image boundaries).
                for (int di = -1; di <= 1; di++)
                {
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        int ni = i + di; // Row index of the neighbor.
                        int nj = j + dj; // Column index of the neighbor.

                        // Check if the neighbor is within the image boundaries.
                        if (ni >= 0 && ni < height && nj >= 0 && nj < width)
                        {
                            neighbors.Add(image[ni * width + nj]); // Add valid neighbor value.
                        }
                    }
                }

                // Sort the collected neighbors and select the median value.
                neighbors.Sort();
                filtered[i * width + j] = neighbors[neighbors.Count / 2];
            }
        }

        return filtered; // Return the filtered binary image.
    }
}
