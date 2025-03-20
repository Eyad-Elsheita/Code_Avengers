using Daenet.Binarizer;
using Daenet.Binarizer.Entities;
using System;
using System.IO;

namespace ImageProcessing
{
    public class ImageProcessor
    {
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