using System;
using System.IO;
using System.Linq;

namespace ImageProcessing
{
    public class ImageLoader
    {
        /// <summary>
        /// Loads a specified number of image files from a folder.
        /// Each image is represented as a flattened 1D binary array.
        /// </summary>
        /// <param name="folderPath">Path to the folder containing the image .txt files.</param>
        /// <param name="numberOfFiles">The number of files to load.</param>
        /// <returns>A jagged array where each element is a binary image array.</returns>
        public static int[][] LoadImageData(string folderPath, int numberOfFiles)
        {
            try
            {
                // Retrieve up to 'numberOfFiles' .txt files from the directory
                var filePaths = Directory.GetFiles(folderPath, "*.txt").Take(numberOfFiles).ToArray();

                // Load each image file and store it as a 1D binary array
                return filePaths.Select(LoadImage).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading images: {ex.Message}");
                return Array.Empty<int[]>(); // Return an empty array to prevent crashes
            }
        }

        /// <summary>
        /// Loads an individual binary image from a .txt file and flattens it into a 1D array.
        /// </summary>
        /// <param name="filePath">Path to the .txt file containing the image.</param>
        /// <returns>A 1D binary array representing the image.</returns>
        public static int[] LoadImage(string filePath)
        {
            try
            {
                // Read all lines from the text file (assuming each row represents a line of binary values)
                return File.ReadAllLines(filePath)
                           .SelectMany(line => line.Select(c => c == '1' ? 1 : 0)) // Convert '1' to 1 and everything else to 0
                           .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image from {filePath}: {ex.Message}");
                return Array.Empty<int>(); // Return an empty array if the file cannot be read
            }
        }

        /// <summary>
        /// Saves a binary image array to a file, storing it in a single comma-separated row.
        /// </summary>
        /// <param name="imageData">A 1D array containing binary image data.</param>
        /// <param name="filePath">The file path where the data will be saved.</param>
        public static void SaveImageDataToFile(int[] imageData, string filePath)
        {
            try
            {
                // Validate the file path to ensure it's not null or empty
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Console.WriteLine("Error: File path is null or empty.");
                    return;
                }

                // Get the directory of the file and ensure it exists
                string directoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath); // Create the directory if it doesn't exist
                }

                // Display the full file path for debugging purposes
                Console.WriteLine($"Saving file to: {Path.GetFullPath(filePath)}");

                // Convert the binary array into a comma-separated string and write to file
                File.WriteAllText(filePath, string.Join(",", imageData));

                Console.WriteLine($"Image data successfully saved to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image data: {ex.Message}");
            }
        }
    }
}
