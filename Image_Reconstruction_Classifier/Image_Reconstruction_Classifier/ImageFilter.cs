public class ImageFilter
{
    public static int[] ApplyMedianFilter(int[] image, int width, int height)
    {
        int[] filtered = new int[image.Length];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                List<int> neighbors = new List<int>();

                // Gather a 3x3 neighborhood (handling image boundaries)
                for (int di = -1; di <= 1; di++)
                {
                    for (int dj = -1; dj <= 1; dj++)
                    {
                        int ni = i + di;
                        int nj = j + dj;

                        if (ni >= 0 && ni < height && nj >= 0 && nj < width)
                        {
                            neighbors.Add(image[ni * width + nj]);
                        }
                    }
                }

                // Sort and take the median
                neighbors.Sort();
                filtered[i * width + j] = neighbors[neighbors.Count / 2];
            }
        }

        return filtered;
    }
}
