using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class SSIM
{
    // Constants for stability
    private const double C1 = (0.01 * 255) * (0.01 * 255);
    private const double C2 = (0.03 * 255) * (0.03 * 255);

    public static double Calculate(int[,] img1, int[,] img2)
    {
        int height = img1.GetLength(0);
        int width = img1.GetLength(1);

        double mu1 = Mean(img1);
        double mu2 = Mean(img2);

        double sigma1Sq = Variance(img1, mu1);
        double sigma2Sq = Variance(img2, mu2);
        double sigma12 = Covariance(img1, img2, mu1, mu2);

        double numerator = (2 * mu1 * mu2 + C1) * (2 * sigma12 + C2);
        double denominator = (mu1 * mu1 + mu2 * mu2 + C1) * (sigma1Sq + sigma2Sq + C2);

        return numerator / denominator;
    }

    private static double Mean(int[,] img)
    {
        double sum = 0;
        foreach (int pixel in img)
            sum += pixel;
        return sum / img.Length;
    }

    private static double Variance(int[,] img, double mean)
    {
        double sum = 0;
        foreach (int pixel in img)
            sum += (pixel - mean) * (pixel - mean);
        return sum / (img.Length - 1);
    }

    private static double Covariance(int[,] img1, int[,] img2, double mean1, double mean2)
    {
        double sum = 0;
        for (int i = 0; i < img1.GetLength(0); i++)
        {
            for (int j = 0; j < img1.GetLength(1); j++)
            {
                sum += (img1[i, j] - mean1) * (img2[i, j] - mean2);
            }
        }
        return sum / (img1.Length - 1);
    }
}