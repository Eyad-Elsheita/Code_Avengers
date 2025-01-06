using System;
using System.IO;
using System.Linq;

namespace ImageProcessing
{
    public class ImageLoader
    {
        // Method to load image data from the folder
        public static int[][] LoadImageData(string folderPath, int numberOfFiles)
        {
            try
            {
                // Get all the .txt files in the folder
                var filePaths = Directory.GetFiles(folderPath, "*.txt").Take(numberOfFiles).ToArray();

                // Initialize the image data array
                int[][] imageData = new int[filePaths.Length][];

                // Load the images from each file
                for (int i = 0; i < filePaths.Length; i++)
                {
                    imageData[i] = LoadImage(filePaths[i]);
                }

                return imageData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading images: {ex.Message}");
                return new int[0][];
            }
        }

        // Method to load an individual image from a file and flatten it
        private static int[] LoadImage(string filePath)
        {
            try
            {
                // Read the content of the text file (binary values)
                string[] lines = File.ReadAllLines(filePath);

                // Convert the content to a binary array, flattening it to a 1D array
                var imageData = lines.SelectMany(line => line.Select(c => c == '1' ? 1 : 0)).ToArray();

                return imageData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image from {filePath}: {ex.Message}");
                return new int[0];
            }
        }

        // Method to save the image data to a file (flattened in one row)
        public static void SaveImageDataToFile(int[] imageData, string filePath)
        {
            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Save the binary image data to a text file (flattened in one row)
                File.WriteAllText(filePath, string.Join(",", imageData.Select(i => i.ToString())));

                Console.WriteLine($"Image data successfully saved to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image data: {ex.Message}");
            }
        }
    }
}
