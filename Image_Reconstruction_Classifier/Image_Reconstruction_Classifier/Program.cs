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
        // Step 1: Load binarized images from the specified folder
        string folderPath = Environment.GetEnvironmentVariable("Training_Image_Binary");
        int numberOfFiles = 2000; // Define the number of images to load
        int[][] imageData = ImageLoader.LoadImageData(folderPath, numberOfFiles);

        if (imageData.Length == 0)
        {
            Console.WriteLine("No images were loaded.");
            return;
        }
        Console.WriteLine($"Loaded {imageData.Length} images.");

        // Step 2: Process images through Spatial Pooler to generate SDR representations
        ImageSpartial.SaveImagesinSpartialPooler();
        Console.WriteLine("Spatial Pooler processing completed.");

        // Step 3: Train HTM Classifier using the processed spatial data
        string spatialFolder = Environment.GetEnvironmentVariable("Training_Image_Spatial") ?? "C:\\try\\Code_Avengers\\Training_Image_Spartial";

        if (!Directory.Exists(spatialFolder))
        {
            Console.WriteLine($"Error: Spatial folder not found: {spatialFolder}");
            return;
        }

        string[] spatialFiles = Directory.GetFiles(spatialFolder, "*.txt");

        var htmClassifier = new MyHtmClassifier();
        for (int i = 0; i < spatialFiles.Length && i < imageData.Length; i++)
        {
            string originalFileName = Path.GetFileNameWithoutExtension(spatialFiles[i]);

            try
            {
                // Read and parse the SDR data
                string sdrData = File.ReadAllText(spatialFiles[i]).Trim();
                if (string.IsNullOrWhiteSpace(sdrData)) continue;

                int[] sdr = sdrData.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(int.Parse)
                    .ToArray();

                // Train HTM classifier with the SDR data and corresponding image index
                htmClassifier.Learn(i, sdr, imageData[i]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {spatialFiles[i]}: {ex.Message}");
            }
        }
        Console.WriteLine("HTM Classifier training completed.");

        // Step 4: Train KNN Classifier using the same spatial data
        var knnClassifier = new KnnClassifier();
        for (int i = 0; i < spatialFiles.Length && i < imageData.Length; i++)
        {
            string originalFileName = Path.GetFileNameWithoutExtension(spatialFiles[i]);

            try
            {
                // Read and parse the SDR data
                string sdrData = File.ReadAllText(spatialFiles[i]).Trim();
                if (string.IsNullOrWhiteSpace(sdrData)) continue;

                int[] sdr = sdrData.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(int.Parse)
                    .ToArray();

                // Train KNN classifier with the SDR data and corresponding image index
                knnClassifier.Train(sdr, i);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {spatialFiles[i]}: {ex.Message}");
            }
        }
        Console.WriteLine("KNN Classifier training completed.");

        // Define paths for saving results
        string htmBinaryFolder = Environment.GetEnvironmentVariable("HTM_Binary_Output_Path");
        string htmVectorFolder = Environment.GetEnvironmentVariable("HTM_Vector_Output_Path");
        string knnBinaryFolder = Environment.GetEnvironmentVariable("KNN_Binary_Output_Path");
        string knnVectorFolder = Environment.GetEnvironmentVariable("KNN_Vector_Output_Path");
        string similarityStatisticsFolder = Environment.GetEnvironmentVariable("Similarity_Statistics");

        // Lists to store similarity results for HTM and KNN classifiers
        List<SimilarityData> htmSimilarityList = new List<SimilarityData>();
        List<SimilarityData> knnSimilarityList = new List<SimilarityData>();

        for (int i = 0; i < spatialFiles.Length && i < imageData.Length; i++)
        {
            string originalFileName = Path.GetFileNameWithoutExtension(spatialFiles[i]);

            // Read SDR data for testing
            int[] testSDR = File.ReadAllText(spatialFiles[i])
                     .Split(',')
                     .Select(s => s.Trim())
                     .Where(s => !string.IsNullOrEmpty(s) && int.TryParse(s, out _))
                     .Select(int.Parse)
                     .ToArray();

            // HTM Reconstruction process
            int[] htmReconstructed = htmClassifier.GetPredictedInputValues(testSDR, 3);
            SaveReconstructedImages(htmReconstructed, originalFileName, htmBinaryFolder, htmVectorFolder);

            // Calculate similarity metrics for HTM classifier
            double htmVectorSimilarity = ImageSimilarity.CalculateCosineSimilarity(imageData[i], htmReconstructed);
            double htmBinarySimilarity = CalculateBinarizedImageSimilarity(imageData[i], htmReconstructed);
            htmSimilarityList.Add(new SimilarityData { PictureName = originalFileName, VectorSimilarityPercentage = htmVectorSimilarity * 100, BinarySimilarityPercentage = htmBinarySimilarity * 100 });

            // KNN Classification and Reconstruction process
            int predictedLabel = knnClassifier.Classify(testSDR, 5); // Using K=5 for nearest neighbors
            int[] knnReconstructed = imageData[predictedLabel]; // Retrieve the original image corresponding to the predicted label
            SaveReconstructedImages(knnReconstructed, originalFileName, knnBinaryFolder, knnVectorFolder);

            // Calculate similarity metrics for KNN classifier
            double knnVectorSimilarity = ImageSimilarity.CalculateCosineSimilarity(imageData[i], knnReconstructed);
            double knnBinarySimilarity = CalculateBinarizedImageSimilarity(imageData[i], knnReconstructed);
            knnSimilarityList.Add(new SimilarityData { PictureName = originalFileName, VectorSimilarityPercentage = knnVectorSimilarity * 100, BinarySimilarityPercentage = knnBinarySimilarity * 100 });
        }

        // Save similarity statistics results in Excel format
        ExcelHelper.SaveSimilarityStatistics(htmSimilarityList, Path.Combine(similarityStatisticsFolder, "HTM_Similarity_Statistics.xlsx"));
        ExcelHelper.SaveSimilarityStatistics(knnSimilarityList, Path.Combine(similarityStatisticsFolder, "KNN_Similarity_Statistics.xlsx"));
        Console.WriteLine("Similarity statistics saved.");
    }

    /// <summary>
    /// Saves reconstructed images in both vectorized and binary formats.
    /// </summary>
    /// <param name="image">Reconstructed image data</param>
    /// <param name="originalFileName">Original image name</param>
    /// <param name="binaryFolder">Path to save binary images</param>
    /// <param name="vectorFolder">Path to save vectorized images</param>
    static void SaveReconstructedImages(int[] image, string originalFileName, string binaryFolder, string vectorFolder)
    {
        string vectorFile = Path.Combine(vectorFolder, $"{originalFileName}_Reconstructed_Vector.txt");
        string binaryFile = Path.Combine(binaryFolder, $"{originalFileName}_Reconstructed_Binary.txt");

        // Save vector representation of the reconstructed image
        File.WriteAllText(vectorFile, string.Join(",", image));

        // Convert and save the reconstructed image in binary format
        File.WriteAllText(binaryFile, ImageSimilarity.ConvertToBinaryMatrix(image, 28));
    }

    /// <summary>
    /// Calculates similarity between original and reconstructed binarized images.
    /// </summary>
    /// <param name="original">Original binarized image</param>
    /// <param name="reconstructed">Reconstructed binarized image</param>
    /// <returns>Similarity percentage</returns>
    static double CalculateBinarizedImageSimilarity(int[] original, int[] reconstructed)
    {
        return original.Zip(reconstructed, (o, r) => o == r ? 1 : 0).Sum() / (double)original.Length;
    }
}
