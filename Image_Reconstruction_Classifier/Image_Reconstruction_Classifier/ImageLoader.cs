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
        public static int[] LoadImage(string filePath)
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
        //public static void SaveImageDataToFile(int[] imageData, string filePath)
        //{
        //    try
        //    {
        //        // Check if filePath is null or empty before proceeding
        //        if (string.IsNullOrEmpty(filePath))
        //        {
        //            Console.WriteLine("Error: File path is null or empty.");
        //            return;
        //        }

        //        // Get the directory path; ensure it's not null by using the null-coalescing operator
        //        string directoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;

        //        // Ensure the directory exists (it won't if directoryPath is empty)
        //        if (!string.IsNullOrEmpty(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }
        //        else
        //        {
        //            Console.WriteLine("Error: Invalid directory path.");
        //            return;
        //        }

        //        // Save the binary image data to a text file (flattened in one row)
        //        File.WriteAllText(filePath, string.Join(",", imageData.Select(i => i.ToString())));

        //        Console.WriteLine($"Image data successfully saved to {filePath}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error saving image data: {ex.Message}");
        //    }
        //}

        public static void SaveImageDataToFile(int[] imageData, string filePath)
        {
            try
            {
                // Check if filePath is null or empty
                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine("Error: File path is null or empty.");
                    return;
                }

                // Get the directory path
                string directoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;

                // Ensure the directory exists if the directory path is not null or empty
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Display the full file path ** 
                Console.WriteLine($"Saving file to: {Path.GetFullPath(filePath)}");

                // Save the binary image data to the file (flattened in one row)
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