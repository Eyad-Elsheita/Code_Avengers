using Image_Reconstruction_Classifier;
using ImageProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        // Step 1: Convert the original images to binarized text files
        ImageProcessor.ConvertImagesToBinary();
        Console.WriteLine("Image binarization completed.");

        // Step 2: Load binarized images
        string folderPath = Environment.GetEnvironmentVariable("Training_Image_Binary")!;
        if (string.IsNullOrEmpty(folderPath))
        {
            folderPath = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Binary";
        }
        int numberOfFiles = 2000;
        int[][] imageData = ImageLoader.LoadImageData(folderPath, numberOfFiles);

        if (imageData.Length == 0)
        {
            Console.WriteLine("No images were loaded.");
            return;
        }
        Console.WriteLine($"Loaded {imageData.Length} images.");

        // Step 3: Process images through Spatial Pooler
        ImageSpartial.SaveImagesinSpartialPooler();
        Console.WriteLine("Spatial Pooler processing completed.");

        // Step 4: Initialize our custom HTM Classifier
        var classifier = new MyHtmClassifier();

        // Step 5: Train the classifier with spatial pooler outputs
        string spatialFolder = Environment.GetEnvironmentVariable("Training_Image_Spartial")!;
        if (string.IsNullOrEmpty(spatialFolder))
        {
            spatialFolder = @"C:\Users\Admin\source\repos\se-cloud-2024-2025\MyWork\Project\Image-Reconstruction-Project-\Training_Image_Spartial";
        }

        string[] spatialFiles = Directory.GetFiles(spatialFolder, "*.txt");

        for (int i = 0; i < spatialFiles.Length && i < imageData.Length; i++)
        {
            try
            {
                // Read SDR from file
                string sdrData = File.ReadAllText(spatialFiles[i]);

                string[] sdrStrings = sdrData
                    .Split(',')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
                    .ToArray();

                if (sdrStrings.All(line => int.TryParse(line, out _)))
                {
                    int[] sdr = sdrStrings.Select(int.Parse).ToArray();
                    classifier.Learn(i, sdr, imageData[i]);
                }
                else
                {
                    Console.WriteLine($"Invalid SDR data in file: {spatialFiles[i]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {spatialFiles[i]}: {ex.Message}");
            }
        }

        Console.WriteLine("HTM Classifier training completed.");

        // Step 6: Initialize a list to store similarity data
        List<SimilarityData> similarityDataList = new List<SimilarityData>();

        // Step 7: Process all images and calculate similarities
        string reconstructedBinaryFolderPath = @"C:\Users\Admin\source\repos\se-cloud-2024-2025\MyWork\Project\Image-Reconstruction-Project-\Reconstructed_Binary_Image";
        string reconstructedVectorFolderPath = @"C:\Users\Admin\source\repos\se-cloud-2024-2025\MyWork\Project\Image-Reconstruction-Project-\Reconstructed_vector_Images";

        for (int i = 0; i < spatialFiles.Length && i < imageData.Length; i++)
        {
            string[] sdrStrings = File.ReadAllText(spatialFiles[i])
                                      .Split(',')
                                      .Select(line => line.Trim())
                                      .Where(line => !string.IsNullOrEmpty(line))
                                      .ToArray();

            if (sdrStrings.All(line => int.TryParse(line, out _)))
            {
                int[] testSDR = sdrStrings.Select(int.Parse).ToArray();
                int[] reconstructedImage = classifier.GetPredictedInputValues(testSDR, 3);

                double vectorSimilarity = ImageSimilarity.CalculateCosineSimilarity(imageData[i], reconstructedImage);
                double binarizedSimilarity = CalculateBinarizedImageSimilarity(imageData[i], reconstructedImage);

                // Save the reconstructed vectorized image
                string originalFileName = Path.GetFileNameWithoutExtension(spatialFiles[i]);

                // Remove the "_spatial" part from the original file name
                string cleanedFileName = originalFileName.Replace("_spatial", "");

                // Save the vector file
                string reconstructedVectorFileName = $"{cleanedFileName}_Reconstructed_Vector.txt";
                string reconstructedVectorFilePath = Path.Combine(reconstructedVectorFolderPath, reconstructedVectorFileName);
                File.WriteAllText(reconstructedVectorFilePath, string.Join(",", reconstructedImage));

                // Save the reconstructed binary image as a 28x28 matrix (without spaces between pixels)
                string reconstructedBinary = ImageSimilarity.ConvertToBinaryMatrix(reconstructedImage, 28);  // Use the proper function for formatting

                // Save to file in the binary image folder
                string reconstructedBinaryFileName = $"{cleanedFileName}_Reconstructed_binary.txt";
                string reconstructedBinaryFilePath = Path.Combine(reconstructedBinaryFolderPath, reconstructedBinaryFileName);
                File.WriteAllText(reconstructedBinaryFilePath, reconstructedBinary);

                // Add the similarity data to the list with rounded percentages
                similarityDataList.Add(new SimilarityData
                {
                    PictureName = cleanedFileName, // Using the cleaned name here
                    VectorSimilarityPercentage = Math.Round(vectorSimilarity * 100, 2), // Rounded to 2 decimals
                    BinarySimilarityPercentage = Math.Round(binarizedSimilarity * 100, 2) // Rounded to 2 decimals
                });
            }
            else
            {
                Console.WriteLine($"Invalid SDR data in test file: {spatialFiles[i]}");
            }
        }



        // Step 8: Save the similarity statistics to an Excel sheet
        string excelFilePath = @"C:\Users\Admin\source\repos\se-cloud-2024-2025\MyWork\Project\Image-Reconstruction-Project-\Similarity statistics\Similarity_Statistics.xlsx";
        ExcelHelper.SaveSimilarityStatistics(similarityDataList, excelFilePath);
        Console.WriteLine($"Similarity statistics saved to {excelFilePath}");
    }

    static double CalculateBinarizedImageSimilarity(int[] originalImage, int[] reconstructedImage)
    {
        if (originalImage.Length != reconstructedImage.Length)
            throw new ArgumentException("Original and reconstructed images must have the same size.");

        int matchingPixels = originalImage.Zip(reconstructedImage, (orig, recon) => orig == recon ? 1 : 0).Sum();
        return (double)matchingPixels / originalImage.Length;
    }
}
