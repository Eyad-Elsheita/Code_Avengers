using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

/// <summary>
/// Provides functionality to convert a binary representation of an image into a PNG file.
/// </summary>
public class BinaryToImageConverter
{
    /// <summary>
    /// Saves a binary image as a PNG file.
    /// </summary>
    /// <param name="binaryImage">
    /// A one-dimensional array representing the binary image where each element is either 0 or 1.
    /// Each value corresponds to a pixel in the final image.
    /// </param>
    /// <param name="width">The width of the target image in pixels.</param>
    /// <param name="height">The height of the target image in pixels.</param>
    /// <param name="outputPath">The file path where the PNG image will be saved.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the binaryImage array is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the length of the binaryImage array does not match the product of width and height.
    /// </exception>
    public static void SaveBinaryAsPng(int[] binaryImage, int width, int height, string outputPath)
    {
        // Validate the input array for null values.
        if (binaryImage == null)
        {
            throw new ArgumentNullException(nameof(binaryImage), "The binary image array cannot be null.");
        }

        // Validate that the array length corresponds to the image dimensions.
        if (binaryImage.Length != width * height)
        {
            throw new ArgumentException("The length of the binary image array must match width × height.");
        }

        // Create a new image instance with the specified dimensions using ImageSharp.
        using (Image<Rgba32> image = new Image<Rgba32>(width, height))
        {
            // Process each element in the binary image array.
            // The loop maps each 1D array index to a 2D pixel coordinate.
            for (int i = 0; i < binaryImage.Length; i++)
            {
                int x = i % width;  // Calculate the horizontal (X) coordinate for the current pixel.
                int y = i / width;  // Calculate the vertical (Y) coordinate for the current pixel.

                // Determine the color of the pixel.
                // A binary value of 1 is interpreted as white (fully opaque), and any other value is black.
                Rgba32 color = binaryImage[i] == 1 ? new Rgba32(255, 255, 255, 255) : new Rgba32(0, 0, 0, 255);
                image[x, y] = color; // Assign the color to the pixel at position (x, y).
            }

            // Save the constructed image as a PNG file to the specified output path.
            image.Save(outputPath);
        }
    }
}
