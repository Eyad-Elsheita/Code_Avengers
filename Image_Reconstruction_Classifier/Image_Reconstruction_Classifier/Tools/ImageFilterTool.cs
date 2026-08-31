using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Image_Reconstruction_Classifier.Tools
{
    /// <summary>
    /// MCP tool that applies a 3x3 median filter to binary images to reduce noise.
    /// </summary>
    [McpServerToolType]
    public class ImageFilterTool
    {
        // ============================================================
        // METHOD 1: Apply Median Filter to Reduce Noise
        // ============================================================
        /// <summary>
        /// Applies a 3x3 median filter to a flattened binary image.
        /// </summary>
        /// <param name="image">Flattened binary image array (0s and 1s).</param>
        /// <param name="width">Width of the image in pixels.</param>
        /// <param name="height">Height of the image in pixels.</param>
        /// <returns>The filtered flattened binary image array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="image"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the array length does not equal width × height.</exception>
        [McpServerTool, Description("Apply a 3x3 median filter to a binary image to reduce noise. Useful before or after reconstruction.")]
        public Task<int[]> ApplyMedianFilter(
            [Description("Flattened binary image array (0s and 1s)")] int[] image,
            [Description("Width of the image in pixels")] int width,
            [Description("Height of the image in pixels")] int height)
        {
            try
            {
                if (image == null)
                    throw new ArgumentNullException(nameof(image), "Image array cannot be null.");

                if (image.Length != width * height)
                    throw new ArgumentException("Image length must equal width × height.");

                int[] filtered = ImageFilter.ApplyMedianFilter(image, width, height);

                CloudLogger.LogInfo("ImageFilterTool", $"Applied median filter to {width}x{height} image");
                return Task.FromResult(filtered);
            }
            catch (Exception ex)
            {
                CloudLogger.LogError("ImageFilterTool", "Error applying median filter", ex);
                throw;
            }
        }
    }
}