using System;
using System.Text;

public class ImageSimilarity
{
    /// <summary>
    /// Computes the cosine similarity between two binary images represented as 1D arrays.
    /// </summary>
    /// <param name="original">The original image in a flattened 1D binary array.</param>
    /// <param name="reconstructed">The reconstructed image in a flattened 1D binary array.</param>
    /// <returns>Cosine similarity value between 0 and 1 (higher means more similar).</returns>
    public static double CalculateCosineSimilarity(int[] original, int[] reconstructed)
    {
        if (original.Length != reconstructed.Length)
            throw new ArgumentException("The images must have the same size.");

        double dotProduct = 0;
        double originalNorm = 0;
        double reconstructedNorm = 0;

        // Compute dot product and vector magnitudes
        for (int i = 0; i < original.Length; i++)
        {
            dotProduct += original[i] * reconstructed[i];
            originalNorm += Math.Pow(original[i], 2);
            reconstructedNorm += Math.Pow(reconstructed[i], 2);
        }

        // Check if norms are zero to avoid division by zero
        if (originalNorm == 0 || reconstructedNorm == 0)
            return 0; // If either vector is all zeros, similarity is 0

        return dotProduct / (Math.Sqrt(originalNorm) * Math.Sqrt(reconstructedNorm));
    }

    /// <summary>
    /// Converts a 1D binary image array into a formatted string representing a 2D matrix.
    /// </summary>
    /// <param name="imageArray">Flattened 1D binary array of the image.</param>
    /// <param name="rowSize">The width (or height) of the square image.</param>
    /// <returns>A string representation of the binary image in matrix form.</returns>
    public static string ConvertToBinaryMatrix(int[] imageArray, int rowSize)
    {
        if (imageArray.Length != rowSize * rowSize)
            throw new ArgumentException($"Image array size must be {rowSize * rowSize} elements for a {rowSize}x{rowSize} matrix.");

        StringBuilder sb = new StringBuilder();

        // Convert 1D array to formatted string with newlines
        for (int i = 0; i < imageArray.Length; i++)
        {
            sb.Append(imageArray[i] == 1 ? "1" : "0");

            // Add a newline after each row to properly format the matrix
            if ((i + 1) % rowSize == 0)
            {
                sb.Append("\n");
            }
        }

        return sb.ToString().Trim(); // Trim to remove last unnecessary newline
    }
}
