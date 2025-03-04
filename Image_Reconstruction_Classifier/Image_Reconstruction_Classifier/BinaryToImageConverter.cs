using System;
using System.Drawing;
using System.IO;

public class BinaryToImageConverter
{
    public static void SaveBinaryAsPng(int[] binaryImage, int width, int height, string outputPath)
    {
        // Create a Bitmap with the specified width and height
        using (Bitmap bitmap = new Bitmap(width, height))
        {
            // Loop through the binary array and set the corresponding pixel value
            for (int i = 0; i < binaryImage.Length; i++)
            {
                int x = i % width;  // Calculate the X coordinate
                int y = i / width;  // Calculate the Y coordinate

                // Set the pixel to either black or white based on the binary value
                Color color = binaryImage[i] == 1 ? Color.White : Color.Black;
                bitmap.SetPixel(x, y, color);
            }

            // Save the Bitmap as a PNG file
            bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
