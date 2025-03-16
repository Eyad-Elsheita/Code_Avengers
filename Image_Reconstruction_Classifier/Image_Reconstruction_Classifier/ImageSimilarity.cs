using System.Text;

public class ImageSimilarity
{
    // Existing code for Cosine Similarity
    public static double CalculateCosineSimilarity(int[] original, int[] reconstructed)
    {
        if (original.Length != reconstructed.Length)
            throw new ArgumentException("The images must have the same size.");

        double dotProduct = 0;
        double originalNorm = 0;
        double reconstructedNorm = 0;

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


    // Add the ConvertToBinaryMatrix method to the same class
    public static string ConvertToBinaryMatrix(int[] imageArray, int rowSize)
    {
        if (imageArray.Length != rowSize * rowSize)
            throw new ArgumentException($"Image array size must be {rowSize * rowSize} elements for a {rowSize}x{rowSize} matrix.");

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < imageArray.Length; i++)
        {
            // Append each pixel value (either 0 or 1)
            sb.Append(imageArray[i] == 1 ? "1" : "0");

            // Every 'rowSize' pixels, go to the next line to format as a matrix
            if ((i + 1) % rowSize == 0)
            {
                sb.Append("\n"); // Adds a newline after each row
            }
        }

        return sb.ToString().Trim(); // Return the formatted string, trim to remove last newline
    }
}