using Daenet.Binarizer;
using Daenet.Binarizer.Entities;
using System;
using System.IO;

namespace ImageProcessing
{
    public class ImageProcessor
    {
        public static void ConvertImagesToBinary()
        {
            // Get the input folder path and create the directory if it doesn't exist
            string inputFolder = Environment.GetEnvironmentVariable("Training_Image_Sample")!;

            // Get the Path for the inputfolder 
            if (string.IsNullOrEmpty(inputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths.");
                // Replace with your default path
                inputFolder = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Sample";
            }

            // Create the input directory if it doesn't exist
            DirectoryInfo inputDirectoryInfo = Directory.CreateDirectory(inputFolder);

            // Get the output folder path and create the directory if it doesn't exist
            string outputFolder = Environment.GetEnvironmentVariable("Training_Image_Binary")!;
            // Ensure that you get the path for the output folder 
            if (string.IsNullOrEmpty(outputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths.");
                // Replace with default path
                outputFolder = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Binary";
            }
            DirectoryInfo outputDirectoryInfo = Directory.CreateDirectory(outputFolder);

            // Get all image files in the input folder
            string[] imageFiles = Directory.GetFiles(inputFolder, "*.png"); // You can change the extension if needed

            foreach (var filePath in imageFiles)
            {
                try
                {
                    // Assuming the filename includes the label (e.g., "3_001.png" where "3" is the label)
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string[] nameParts = fileName.Split('_');  // Assuming "label_index" format

                    if (nameParts.Length < 2)
                    {
                        Console.WriteLine($"Skipping invalid file name: {filePath}");
                        continue;  // Skip files that don't match the expected format
                    }

                    // Get label (assuming the label is the first part of the file name)
                    string label = nameParts[0];
                    string index = nameParts[1];

                    // Set the output file name and path with the label and index
                    string outputFileName = $"{label}_{index}_binarized.txt";
                    string outputFilePath = Path.Combine(outputFolder, outputFileName);

                    // Ensure the output folder exists, create it if not
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }

                    // Set binarizer parameters as required
                    var binarizerParams = new BinarizerParams
                    {
                        InputImagePath = filePath,
                        OutputImagePath = outputFilePath,
                        GreyScale = true, // or RGB scale depending on your need
                        CreateCode = false, // Set to true to create code file
                    };

                    // Run the binarizer to generate the .txt file
                    var imageBinarizer = new ImageBinarizer(binarizerParams);
                    imageBinarizer.Run();
                    Console.WriteLine($"Image {filePath} binarized and saved to {outputFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing image {filePath}: {ex.Message}");
                }
            }
        }
    }
}
