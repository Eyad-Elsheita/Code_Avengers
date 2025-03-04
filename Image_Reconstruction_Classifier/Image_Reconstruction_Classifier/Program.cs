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
        // ==================================================
        // ======== PREPROCESSING & TRAINING SETUP ==========
        // ==================================================

        // Step 1: Convert the original images to binarized text files and load them
        ImageProcessor.ConvertImagesToBinary();
        Console.WriteLine("Image binarization completed.");
        string folderPath = Environment.GetEnvironmentVariable("Training_Image_Binary");
        int numberOfFiles = 10000; // training dataset size
        int[][] imageData = ImageLoader.LoadImageData(folderPath, numberOfFiles);

        if (imageData.Length == 0)
        {
            Console.WriteLine("No images were loaded.");
            return;
        }
        Console.WriteLine($"Loaded {imageData.Length} training images.");

        // Step 2: Vectorize training images and save them
        string trainingLoaderFolder = Environment.GetEnvironmentVariable("Training_Image_Loader") ?? "Training_Image_Loader";
        Directory.CreateDirectory(trainingLoaderFolder);
        string[] trainingBinaryFiles = Directory.GetFiles(folderPath, "*.txt");

        foreach (var file in trainingBinaryFiles)
        {
            int[] vectorized = ImageLoader.LoadImage(file);
            string fileName = Path.GetFileNameWithoutExtension(file);
            string[] nameParts = fileName.Split('_');
            if (nameParts.Length < 3)
            {
                Console.WriteLine($"Invalid filename format: {fileName}");
                continue;
            }
            string code = nameParts[0];
            string label = nameParts[1];
            string vectorizedFileName = $"{code}_{label}_vectorized.txt";
            ImageLoader.SaveImageDataToFile(vectorized, Path.Combine(trainingLoaderFolder, vectorizedFileName));
        }
        Console.WriteLine($"Vectorized training images saved to {trainingLoaderFolder}");

        // Process images through Spatial Pooler
        ImageSpartial.SaveImagesinSpartialPooler();
        Console.WriteLine("Spatial Pooler processing completed.");

        // ==================================================
        // ======== TRAINING PHASE: GROUP BY OBJECT TYPE =====
        // ==================================================

        string spatialFolder = Environment.GetEnvironmentVariable("Training_Image_Spatial") ?? "C:\\try\\Code_Avengers\\Training_Image_Spartial";
        if (!Directory.Exists(spatialFolder))
        {
            Console.WriteLine($"Error: Spatial folder not found: {spatialFolder}");
            return;
        }

        // Get spatial files with their original indices (assumed to match imageData order)
        var spatialFilesWithIndex = Directory.GetFiles(spatialFolder, "*.txt")
            .Select((file, index) => new { file, index })
            .ToArray();

        // Dictionaries to store classifiers per object type (keys "0" to "9")
        Dictionary<string, MyHtmClassifier> htmClassifiers = new Dictionary<string, MyHtmClassifier>();
        Dictionary<string, KnnClassifier> knnClassifiers = new Dictionary<string, KnnClassifier>();

        // Loop through each object type (0-9)
        for (int type = 0; type < 10; type++)
        {
            string typeStr = type.ToString();
            // Filter training files whose filename starts with the current object type (e.g., "0_...")
            var filesForType = spatialFilesWithIndex
                .Where(x => Path.GetFileNameWithoutExtension(x.file).StartsWith(typeStr + "_"))
                .ToArray();

            if (!filesForType.Any())
            {
                Console.WriteLine($"No training files for object type {typeStr}");
                continue;
            }

            Console.WriteLine($"Training classifiers for object type {typeStr} with {filesForType.Length} images.");

            // Create new classifier instances for this object type
            var htmClassifierForType = new MyHtmClassifier();
            var knnClassifierForType = new KnnClassifier();

            // Train classifiers only on images of the current object type
            foreach (var item in filesForType)
            {
                try
                {
                    string sdrData = File.ReadAllText(item.file).Trim();
                    if (string.IsNullOrWhiteSpace(sdrData))
                        continue;
                    int[] sdr = sdrData.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(int.Parse)
                        .ToArray();

                    int idx = item.index;
                    if (idx >= imageData.Length)
                        continue;

                    // Train both classifiers using the SDR and the corresponding original image
                    htmClassifierForType.Learn(idx, sdr, imageData[idx]);
                    knnClassifierForType.Train(sdr, idx);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {item.file}: {ex.Message}");
                }
            }

            // Store the trained classifiers in dictionaries keyed by object type
            htmClassifiers[typeStr] = htmClassifierForType;
            knnClassifiers[typeStr] = knnClassifierForType;
            Console.WriteLine($"Training completed for object type {typeStr}");
        }
        Console.WriteLine("All object type training completed.");

        // ==================================================
        // ======== TESTING PHASE: GROUP BY OBJECT TYPE =====
        // ==================================================

        // Preprocess test images (binarize and vectorize) as before
        string testImagesFolder = Environment.GetEnvironmentVariable("Test_Images") ?? "Test_Images";
        string testBinaryFolder = Environment.GetEnvironmentVariable("Test_Image_Binary") ?? "Test_Image_Binary";
        string testSpatialFolder = Environment.GetEnvironmentVariable("Test_Image_Spatial") ?? "Test_Image_Spatial";

        Environment.SetEnvironmentVariable("Training_Image_Sample", testImagesFolder);
        Environment.SetEnvironmentVariable("Training_Image_Binary", testBinaryFolder);
        ImageProcessor.ConvertImagesToBinary();

        string testLoaderFolder = Environment.GetEnvironmentVariable("Test_Image_Loader") ?? "Test_Image_Loader";
        Directory.CreateDirectory(testLoaderFolder);
        string[] testBinaryFiles = Directory.GetFiles(testBinaryFolder, "*.txt");
        foreach (var file in testBinaryFiles)
        {
            int[] vectorized = ImageLoader.LoadImage(file);
            string fileName = Path.GetFileNameWithoutExtension(file);
            string[] nameParts = fileName.Split('_');
            if (nameParts.Length < 3)
            {
                Console.WriteLine($"Invalid filename format: {fileName}");
                continue;
            }
            string code = nameParts[0];
            string label = nameParts[1];
            string vectorizedFileName = $"{code}_{label}_vectorized.txt";
            ImageLoader.SaveImageDataToFile(vectorized, Path.Combine(testLoaderFolder, vectorizedFileName));
        }
        Console.WriteLine($"Vectorized test images saved to {testLoaderFolder}");

        Environment.SetEnvironmentVariable("Test_Image_Binary", testBinaryFolder);
        Environment.SetEnvironmentVariable("Test_Image_Spatial", testSpatialFolder);
        ImageSpartial.ProcessTestImagesSpatial();
        Console.WriteLine("Test dataset processing completed.");

        // Get test spatial files with indices
        var testSpatialFilesWithIndex = Directory.GetFiles(testSpatialFolder, "*.txt")
            .Select((file, index) => new { file, index })
            .ToArray();
        int[][] testImageData = ImageLoader.LoadImageData(testBinaryFolder, testSpatialFilesWithIndex.Length);

        // Define folders to save test reconstruction results for HTM and KNN
        string htmTestBinaryFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Binary_HTM") ?? "Test_Images_Reconstructed_Binary_HTM";
        string htmTestVectorFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Vector_HTM") ?? "Test_Images_Reconstructed_Vector_HTM";
        string knnTestBinaryFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Binary_KNN") ?? "Test_Images_Reconstructed_Binary_KNN";
        string knnTestVectorFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Vector_KNN") ?? "Test_Images_Reconstructed_Vector_KNN";
        Directory.CreateDirectory(htmTestBinaryFolder);
        Directory.CreateDirectory(htmTestVectorFolder);
        Directory.CreateDirectory(knnTestBinaryFolder);
        Directory.CreateDirectory(knnTestVectorFolder);

        // Define folders to save combined reconstruction results
        string combinedTestBinaryFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Binary_Combined") ?? "Test_Images_Reconstructed_Binary_Combined";
        string combinedTestVectorFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Vector_Combined") ?? "Test_Images_Reconstructed_Vector_Combined";
        Directory.CreateDirectory(combinedTestBinaryFolder);
        Directory.CreateDirectory(combinedTestVectorFolder);

        List<SimilarityData> htmTestSimilarities = new List<SimilarityData>();
        List<SimilarityData> knnTestSimilarities = new List<SimilarityData>();
        List<SimilarityData> combinedTestSimilarities = new List<SimilarityData>();

        // Process test images by object type
        for (int type = 0; type < 10; type++)
        {
            string typeStr = type.ToString();
            var testFilesForType = testSpatialFilesWithIndex
                .Where(x => Path.GetFileNameWithoutExtension(x.file).StartsWith(typeStr + "_"))
                .ToArray();
            if (!testFilesForType.Any())
            {
                Console.WriteLine($"No test files for object type {typeStr}");
                continue;
            }
            Console.WriteLine($"Testing classifiers for object type {typeStr} with {testFilesForType.Length} images.");

            if (!htmClassifiers.ContainsKey(typeStr) || !knnClassifiers.ContainsKey(typeStr))
            {
                Console.WriteLine($"No trained classifiers available for object type {typeStr}");
                continue;
            }
            var htmClassifierForType = htmClassifiers[typeStr];
            var knnClassifierForType = knnClassifiers[typeStr];

            foreach (var item in testFilesForType)
            {
                string originalFileName = Path.GetFileNameWithoutExtension(item.file);
                try
                {
                    int[] testSdr = File.ReadAllText(item.file)
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s) && int.TryParse(s, out _))
                        .Select(int.Parse)
                        .ToArray();

                    // HTM Test Reconstruction
                    int[] htmTestReconstructed = htmClassifierForType.GetPredictedInputValues(testSdr, 3);
                    SaveReconstructedImages(htmTestReconstructed, originalFileName, htmTestBinaryFolder, htmTestVectorFolder);
                    double htmVectorSim = ImageSimilarity.CalculateCosineSimilarity(testImageData[item.index], htmTestReconstructed);
                    double htmBinarySim = CalculateBinarizedImageSimilarity(testImageData[item.index], htmTestReconstructed);
                    htmTestSimilarities.Add(new SimilarityData
                    {
                        PictureName = originalFileName,
                        VectorSimilarityPercentage = htmVectorSim * 100,
                        BinarySimilarityPercentage = htmBinarySim * 100
                    });

                    // KNN Test Reconstruction
                    int predictedLabel = knnClassifierForType.Classify(testSdr, 5);
                    int[] knnTestReconstructed = imageData[predictedLabel]; // using corresponding training image
                    SaveReconstructedImages(knnTestReconstructed, originalFileName, knnTestBinaryFolder, knnTestVectorFolder);
                    double knnVectorSim = ImageSimilarity.CalculateCosineSimilarity(testImageData[item.index], knnTestReconstructed);
                    double knnBinarySim = CalculateBinarizedImageSimilarity(testImageData[item.index], knnTestReconstructed);
                    knnTestSimilarities.Add(new SimilarityData
                    {
                        PictureName = originalFileName,
                        VectorSimilarityPercentage = knnVectorSim * 100,
                        BinarySimilarityPercentage = knnBinarySim * 100
                    });

                    int[] combinedReconstructed = new int[htmTestReconstructed.Length];
                    for (int j = 0; j < htmTestReconstructed.Length; j++)
                    {
                        combinedReconstructed[j] = (htmTestReconstructed[j] + knnTestReconstructed[j]) >= 1 ? 1 : 0;
                    }

                    // Save the combined binary image as PNG in the specified environment variable path
                    string combinedImagesFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Combined");

                    if (string.IsNullOrEmpty(combinedImagesFolder))
                    {
                        Console.WriteLine("Environment variable 'Test_Images_Reconstructed_Combined' is not set.");
                    }
                    else
                    {
                        int width = 28;  // Example width (adjust based on your dataset)
                        int height = 28; // Example height (adjust based on your dataset)
                        string outputCombinedFilePath = Path.Combine(combinedImagesFolder, originalFileName + "_Combined.png");

                        // Save the combined binary image using BinaryToImageConverter
                        BinaryToImageConverter.SaveBinaryAsPng(combinedReconstructed, width, height, outputCombinedFilePath);
                    }

                    // Save the reconstructed binary and vector images (your current code remains unchanged)
                    SaveReconstructedImages(combinedReconstructed, originalFileName + "_Combined", combinedTestBinaryFolder, combinedTestVectorFolder);
                    double combinedVectorSim = ImageSimilarity.CalculateCosineSimilarity(testImageData[item.index], combinedReconstructed);
                    double combinedBinarySim = CalculateBinarizedImageSimilarity(testImageData[item.index], combinedReconstructed);
                    combinedTestSimilarities.Add(new SimilarityData
                    {
                        PictureName = originalFileName,
                        VectorSimilarityPercentage = combinedVectorSim * 100,
                        BinarySimilarityPercentage = combinedBinarySim * 100
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing test image {originalFileName}: {ex.Message}");
                }
            }
        }

        // Save test similarity statistics
        string similarityStatisticsFolder = Environment.GetEnvironmentVariable("Similarity_Statistics");
        ExcelHelper.SaveSimilarityStatistics(htmTestSimilarities,
            Path.Combine(similarityStatisticsFolder, "HTM_Test_Similarity_Statistics.xlsx"));
        ExcelHelper.SaveSimilarityStatistics(knnTestSimilarities,
            Path.Combine(similarityStatisticsFolder, "KNN_Test_Similarity_Statistics.xlsx"));
        ExcelHelper.SaveSimilarityStatistics(combinedTestSimilarities,
            Path.Combine(similarityStatisticsFolder, "Combined_Test_Similarity_Statistics.xlsx"));
        Console.WriteLine("Test dataset processing completed.");
    }

    /// <summary>
    /// Saves reconstructed images in both vectorized and binary formats.
    /// </summary>
    static void SaveReconstructedImages(int[] image, string originalFileName, string binaryFolder, string vectorFolder)
    {
        string vectorFile = Path.Combine(vectorFolder, $"{originalFileName}_Reconstructed_Vector.txt");
        string binaryFile = Path.Combine(binaryFolder, $"{originalFileName}_Reconstructed_Binary.txt");

        File.WriteAllText(vectorFile, string.Join(",", image));
        File.WriteAllText(binaryFile, ImageSimilarity.ConvertToBinaryMatrix(image, 28));
    }

    /// <summary>
    /// Calculates similarity between original and reconstructed binarized images.
    /// </summary>
    static double CalculateBinarizedImageSimilarity(int[] original, int[] reconstructed)
    {
        return original.Zip(reconstructed, (o, r) => o == r ? 1 : 0).Sum() / (double)original.Length;
    }
}
