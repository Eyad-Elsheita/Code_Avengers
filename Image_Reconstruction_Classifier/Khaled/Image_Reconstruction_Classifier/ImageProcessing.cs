using Daenet.Binarizer;
using Daenet.Binarizer.Entities;
using System;
using System.IO;

namespace ImageProcessing
{
    public class ImageProcessor
    {
        /// <summary>
        /// Converts all PNG images in the input folder to binary format and saves them as text files.
        /// Uses parameters first, falls back to environment variables if parameters are not provided.
        /// </summary>
        public static void ConvertImagesToBinary(string inputFolderPath, string outputFolderPath)
        {
            // Use parameters first, fall back to environment variables if not provided
            string inputFolder = !string.IsNullOrEmpty(inputFolderPath)
                ? inputFolderPath
                : Environment.GetEnvironmentVariable("Training_Image_Sample")
                  ?? throw new InvalidOperationException("Input folder path is not set. Provide it as a parameter or set the 'Training_Image_Sample' environment variable.");

            string outputFolder = !string.IsNullOrEmpty(outputFolderPath)
                ? outputFolderPath
                : Environment.GetEnvironmentVariable("Training_Image_Binary")
                  ?? throw new InvalidOperationException("Output folder path is not set. Provide it as a parameter or set the 'Training_Image_Binary' environment variable.");

            // Ensure that the directories exist (create them if necessary)
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(outputFolder);

            // Get all PNG image files from the input folder
            string[] imageFiles = Directory.GetFiles(inputFolder, "*.png");

            // Check if there are any files to process
            if (imageFiles.Length == 0)
            {
                Console.WriteLine("Input directory is empty. No files to process.");
                return;
            }

            // Iterate through each image file and process it
            foreach (var filePath in imageFiles)
            {
                try
                {
                    // Extract filename without extension (assuming format: label_index.png)
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string[] nameParts = fileName.Split('_');

                    // Validate filename format (must contain at least "label_index")
                    if (nameParts.Length < 2)
                    {
                        Console.WriteLine($"Skipping invalid file name: {filePath}");
                        continue;
                    }

                    string label = nameParts[0]; // Extract label (e.g., category)
                    string index = nameParts[1]; // Extract index (e.g., image number)

                    // Define output filename with a "_binarized" suffix
                    string outputFileName = $"{label}_{index}_binarized.txt";
                    string outputFilePath = Path.Combine(outputFolder, outputFileName);

                    // Set binarization parameters
                    var binarizerParams = new BinarizerParams
                    {
                        InputImagePath = filePath,
                        OutputImagePath = outputFilePath,
                        GreyScale = true,  // Convert to grayscale before binarizing
                        CreateCode = false // Do not generate additional binarization code
                    };

                    // Perform binarization
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
