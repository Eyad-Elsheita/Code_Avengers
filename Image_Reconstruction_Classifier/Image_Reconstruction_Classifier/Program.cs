using System;
using System.Linq; // Make sure to include this for the .Take method
using System.IO;
using ImageProcessing;
using NeoCortexApi;
using Image_Reconstruction_Classifier;

class Program
{
    static void Main(string[] args)
    {
        // Step 1: Convert the original images to binarized text files
        ImageProcessor.ConvertImagesToBinary();
        Console.WriteLine("Image binarization completed.");

        // Step 2: Define the folder path where the binary image text files are located
        string folderPath = Environment.GetEnvironmentVariable("Training_Image_Binary")!;

        // Ensure that you get the path for the output folder 
        if (string.IsNullOrEmpty(folderPath))
        {
            Console.WriteLine("Environment variables not set. Using default paths.");
            // Replace with default path
            folderPath = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Binary";
        }

        DirectoryInfo directoryInfo = Directory.CreateDirectory(folderPath);

        // Step 3: Define how many files to load (you can change this number as needed)
        int numberOfFiles = 2000;

        // Step 4: Initialize the ImageLoader
        // ImageLoader imageLoader = new ImageLoader(); // No need to initialize since we're using static methods

        // Step 5: Load the binarized image data from the folder
        int[][] imageData = ImageLoader.LoadImageData(folderPath, numberOfFiles);

        // Step 6: Check if images are loaded successfully
        if (imageData.Length == 0)
        {
            Console.WriteLine("No images were loaded.");
            return;
        }

        // Step 7: Example processing using the loaded data
        Console.WriteLine($"Loaded {imageData.Length} images.");

        // Step 8: Save each image data to a separate file
        // Get the output folder path from the environment variable
        string outputFolder = Environment.GetEnvironmentVariable("Training_Image_Loader")!;

        // Ensure that you get the path for the output folder 
        if (string.IsNullOrEmpty(outputFolder))
        {
            Console.WriteLine("Environment variables not set for Output Folder. Using default paths.");
            // Replace with default path
            outputFolder = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Loader";
        }

        // Create the directory if it doesn't exist
        DirectoryInfo outputDirectoryInfo = Directory.CreateDirectory(outputFolder);

        // Save the image data
        // Get all the binarized image file paths
        var binarizedFiles = Directory.GetFiles(folderPath, "*.txt").Take(numberOfFiles).ToArray();

        // Save the image data
        for (int i = 0; i < imageData.Length; i++)
        {
            // Get the original file name without extension
            string originalFileName = Path.GetFileNameWithoutExtension(binarizedFiles[i]);

            // Split the original file name into code and label (assuming you want the code_label format)
            string[] nameParts = originalFileName.Split('_');
            string code = nameParts.Length > 0 ? nameParts[0] : "unknown";
            string label = nameParts.Length > 1 ? nameParts[1] : "unknown";

            // Combine the output folder path with the new vectorized file name
            string outputFilePath = Path.Combine(outputFolder, $"{code}_{label}_vectorized.txt");

            // Save the image data to the output path
            ImageLoader.SaveImageDataToFile(imageData[i], outputFilePath);
            Console.WriteLine($"Saved vectorized image data to: {outputFilePath}");
        }


        // Step 9: SpatialPooler Output 
        ImageSpartial.SaveImagesinSpartialPooler();
        Console.WriteLine("SpartialPooler completed.");

        // Step 9: Initialize the SpatialPooler
        //SpatialPooler spatialPooler = new SpatialPooler();

        // Step 10: Process each image data as input vector
        //foreach (var inputVector in imageData)
        //{
        //    //Ensure the input vector is in the expected format (0s and 1s)
        //    // Classify or process the input vector
        //    int[] activeColumns = spatialPooler.Compute(inputVector, true); // Set learn to true if you want to learn

        //    //Output or further process the result as needed
        //    Console.WriteLine($"Active Columns: {string.Join(", ", activeColumns)}");
        //}

        Console.WriteLine("Processing completed.");
    }
}