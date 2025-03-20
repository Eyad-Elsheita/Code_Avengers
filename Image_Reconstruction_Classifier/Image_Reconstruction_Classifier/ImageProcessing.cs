using Daenet.Binarizer;
using Daenet.Binarizer.Entities;
using System;
using System.IO;

namespace ImageProcessing
{
    public class ImageProcessor
    {
        //public static void ConvertImagesToBinary(String inputFolderPath, string outputFolderPath)
        //{
        //    // Get the input folder path and create the directory if it doesn't exist
        //    string inputFolder = Environment.GetEnvironmentVariable(inputFolderPath)!;

        //    // Create the input directory if it doesn't exist
        //    DirectoryInfo inputDirectoryInfo = Directory.CreateDirectory(inputFolder);

        //    // Get the output folder path and create the directory if it doesn't exist
        //    string outputFolder = Environment.GetEnvironmentVariable(outputFolderPath)!;

        //    DirectoryInfo outputDirectoryInfo = Directory.CreateDirectory(outputFolder);

        //    // Get all image files in the input folder
        //    string[] imageFiles = Directory.GetFiles(inputFolder, "*.png"); // You can change the extension if needed

        //    foreach (var filePath in imageFiles)
        //    {
        //        try
        //        {
        //            // Assuming the filename includes the label (e.g., "3_001.png" where "3" is the label)
        //            string fileName = Path.GetFileNameWithoutExtension(filePath);
        //            string[] nameParts = fileName.Split('_');  // Assuming "label_index" format

        //            if (nameParts.Length < 2)
        //            {
        //                Console.WriteLine($"Skipping invalid file name: {filePath}");
        //                continue;  // Skip files that don't match the expected format
        //            }

        //            // Get label (assuming the label is the first part of the file name)
        //            string label = nameParts[0];
        //            string index = nameParts[1];

        //            // Set the output file name and path with the label and index
        //            string outputFileName = $"{label}_{index}_binarized.txt";
        //            string outputFilePath = Path.Combine(outputFolder, outputFileName);

        //            // Ensure the output folder exists, create it if not
        //            if (!Directory.Exists(outputFolder))
        //            {
        //                Directory.CreateDirectory(outputFolder);
        //            }

        //            // Set binarizer parameters as required
        //            var binarizerParams = new BinarizerParams
        //            {
        //                InputImagePath = filePath,
        //                OutputImagePath = outputFilePath,
        //                GreyScale = true, // or RGB scale depending on your need
        //                CreateCode = false, // Set to true to create code file
        //            };

        //            // Run the binarizer to generate the .txt file
        //            var imageBinarizer = new ImageBinarizer(binarizerParams);
        //            imageBinarizer.Run();
        //            Console.WriteLine($"Image {filePath} binarized and saved to {outputFilePath}");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error processing image {filePath}: {ex.Message}");
        //        }
        //    }
        //}

        public static void ConvertImagesToBinary(string inputFolderPath, string outputFolderPath)
        {
            string inputFolder = Environment.GetEnvironmentVariable("Training_Image_Sample")!;
            string outputFolder = Environment.GetEnvironmentVariable("Training_Image_Binary")!;

            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(outputFolder);

            string[] imageFiles = Directory.GetFiles(inputFolder, "*.png");

            if (imageFiles.Length == 0)
            {
                Console.WriteLine("Input directory is empty. No files to process.");
                return;
            }

            foreach (var filePath in imageFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string[] nameParts = fileName.Split('_');

                    if (nameParts.Length < 2)
                    {
                        Console.WriteLine($"Skipping invalid file name: {filePath}");
                        continue;
                    }

                    string label = nameParts[0];
                    string index = nameParts[1];
                    string outputFileName = $"{label}_{index}_binarized.txt";
                    string outputFilePath = Path.Combine(outputFolder, outputFileName);

                    var binarizerParams = new BinarizerParams
                    {
                        InputImagePath = filePath,
                        OutputImagePath = outputFilePath,
                        GreyScale = true,
                        CreateCode = false
                    };

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