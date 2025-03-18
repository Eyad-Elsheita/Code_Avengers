using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

public class BinaryToImageConverter
{
    //public static void SaveBinaryAsPng(int[] binaryImage, int width, int height, string outputPath)
    //{
    //    // Create an image with the specified width and height
    //    using (Image<Rgba32> image = new Image<Rgba32>(width, height))
    //    {
    //        // Loop through the binary array and set the corresponding pixel value
    //        for (int i = 0; i < binaryImage.Length; i++)
    //        {
    //            int x = i % width;  // Calculate the X coordinate
    //            int y = i / width;  // Calculate the Y coordinate

    //            // Set the pixel to either black or white based on the binary value
    //            Rgba32 color = binaryImage[i] == 1 ? new Rgba32(255, 255, 255, 255) : new Rgba32(0, 0, 0, 255); // Manually specify white and black
    //            image[x, y] = color;
    //        }

    //        // Save the image as a PNG file
    //        image.Save(outputPath); // No need for specifying image format
    //    }
    //}

    public static void SaveBinaryAsPng(int[] binaryImage, int width, int height, string outputPath)
    {
        // Validate input
        if (binaryImage == null)
        {
            throw new ArgumentNullException(nameof(binaryImage), "The binary image array cannot be null.");
        }
        if (binaryImage.Length != width * height)
        {
            throw new ArgumentException("The length of the binary image array must match width × height.");
        }

        // Create an image with the specified width and height
        using (Image<Rgba32> image = new Image<Rgba32>(width, height))
        {
            // Loop through the binary array and set the corresponding pixel value
            for (int i = 0; i < binaryImage.Length; i++)
            {
                int x = i % width;  // Calculate the X coordinate
                int y = i / width;  // Calculate the Y coordinate

                // Set the pixel to either black or white based on the binary value
                Rgba32 color = binaryImage[i] == 1 ? new Rgba32(255, 255, 255, 255) : new Rgba32(0, 0, 0, 255); // Manually specify white and black
                image[x, y] = color;
            }

            // Save the image as a PNG file
            image.Save(outputPath); // No need for specifying image format
        }
    }

}
