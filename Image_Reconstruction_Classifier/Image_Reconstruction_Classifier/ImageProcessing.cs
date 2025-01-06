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
            DirectoryInfo inputDirectoryInfo = Directory.CreateDirectory(inputFolder);

            // Get the output folder path and create the directory if it doesn't exist
            string outputFolder = Environment.GetEnvironmentVariable("Training_Image_Binary")!;
            DirectoryInfo outputDirectoryInfo = Directory.CreateDirectory(outputFolder);

            // Get all image files in the input folder
            string[] imageFiles = Directory.GetFiles(inputFolder, "*.png"); // You can change the extension if needed

            foreach (var filePath in imageFiles)
            {
                try
                {
                    // Set the output file name and path
                    string outputFileName = Path.GetFileNameWithoutExtension(filePath) + "_binarized.txt";
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
                        CreateCode = false, // Set to true if you want to create code file
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
