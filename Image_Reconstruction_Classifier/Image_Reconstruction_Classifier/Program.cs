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
        string folderPath = Environment.GetEnvironmentVariable("Training_Image_Binary") ?? "Default_Training_Path";
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"Error: Folder path {folderPath} does not exist.");
            return;
        }
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

                    // Assume the image dimensions are defined as:
                    int width = 28;  // adjust if needed
                    int height = 28; // adjust if needed

                    // Combined Reconstruction using confidence weighting and Gaussian weighted local voting
                    double totalConfidence = htmVectorSim + knnVectorSim;
                    if (totalConfidence == 0)
                        totalConfidence = 1; // avoid division by zero

                    int[] combinedReconstructed = new int[htmTestReconstructed.Length];

                    for (int j = 0; j < htmTestReconstructed.Length; j++)
                    {
                        // Compute confidence weighted probability for this pixel
                        double confWeightedProb = (htmVectorSim * htmTestReconstructed[j] + knnVectorSim * knnTestReconstructed[j]) / totalConfidence;

                        if (htmTestReconstructed[j] != knnTestReconstructed[j])
                        {
                            // Use Gaussian weighted local vote from both reconstructions
                            double htmLocalVote = GetGaussianWeightedLocalVote(htmTestReconstructed, j, width, height);
                            double knnLocalVote = GetGaussianWeightedLocalVote(knnTestReconstructed, j, width, height);
                            double localMajority = (htmLocalVote + knnLocalVote) / 2.0;

                            // Combine the confidence weighted probability with the Gaussian weighted local decision
                            double combinedDecision = (confWeightedProb + localMajority) / 2.0;
                            combinedReconstructed[j] = combinedDecision >= 0.5 ? 1 : 0;
                        }
                        else
                        {
                            combinedReconstructed[j] = confWeightedProb >= 0.5 ? 1 : 0;
                        }
                    }

                    // ===== Apply the Median Filter =====
                    // Use the median filter to post-process the combined reconstruction
                    int[] postProcessedImage = ImageFilter.ApplyMedianFilter(combinedReconstructed, width, height);

                    // --- Helper Method: GetPixelValueFromNeighborhood ---
                    // This method examines the 3x3 neighborhood around the given pixel (j)
                    // in a flat array representing an image of dimensions width x height.
                    //static int GetPixelValueFromNeighborhood(int[] image, int pixelIndex, int width, int height)
                    //{
                    //  int row = pixelIndex / width;
                    //int col = pixelIndex % width;
                    //int onesCount = 0;
                    //int count = 0;

                    // Loop over a 3x3 window (handling boundaries)
                    //for (int i = Math.Max(0, row - 1); i <= Math.Min(height - 1, row + 1); i++)
                    //{
                    //  for (int j = Math.Max(0, col - 1); j <= Math.Min(width - 1, col + 1); j++)
                    //{
                    //  onesCount += image[i * width + j];
                    //count++;
                    //}
                    //}
                    // Return 1 if the majority in the window are 1's; otherwise 0.
                    //return (onesCount > count / 2) ? 1 : 0;
                    //}

                    static double GetGaussianWeightedLocalVote(int[] image, int pixelIndex, int width, int height)
                    {
                        // Gaussian kernel for a 3x3 window (you can adjust these weights if needed)
                        double[,] kernel = new double[,]
                        {
                            { 0.075, 0.124, 0.075 },
                            { 0.124, 0.204, 0.124 },
                            { 0.075, 0.124, 0.075 }
                        };

                        int row = pixelIndex / width;
                        int col = pixelIndex % width;
                        double weightedSum = 0;
                        double totalWeight = 0;

                        // Loop over the 3x3 window, applying the Gaussian weights
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                int r = row + i;
                                int c = col + j;
                                if (r >= 0 && r < height && c >= 0 && c < width)
                                {
                                    double weight = kernel[i + 1, j + 1];
                                    weightedSum += weight * image[r * width + c];
                                    totalWeight += weight;
                                }
                            }
                        }

                        // Return a weighted average as a double between 0 and 1.
                        return weightedSum / totalWeight;
                    }

                    // Save the combined binary image as PNG in the specified environment variable path
                    string combinedImagesFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Combined") ?? "";
                    if (string.IsNullOrEmpty(combinedImagesFolder))
                    {
                        Console.WriteLine("Error: Environment variable 'Test_Images_Reconstructed_Combined' is not set.");
                        return;
                    }


                    if (string.IsNullOrEmpty(combinedImagesFolder))
                    {
                        Console.WriteLine("Environment variable 'Test_Images_Reconstructed_Combined' is not set.");
                    }
                    else
                    {
                        int imageWidth = 28;  // Example width (adjust based on your dataset)
                        int imageHeight = 28; // Example height (adjust based on your dataset)
                        string outputCombinedFilePath = Path.Combine(combinedImagesFolder, originalFileName + "_Combined.png");

                        // Save the post-processed image using BinaryToImageConverter
                        BinaryToImageConverter.SaveBinaryAsPng(postProcessedImage, imageWidth, imageHeight, outputCombinedFilePath);
                    }

                    // Save the reconstructed binary and vector images (using the post-processed image)
                    SaveReconstructedImages(postProcessedImage, originalFileName + "_Combined", combinedTestBinaryFolder, combinedTestVectorFolder);

                    // Calculate similarity using the post-processed image
                    double combinedVectorSim = ImageSimilarity.CalculateCosineSimilarity(testImageData[item.index], postProcessedImage);
                    double combinedBinarySim = CalculateBinarizedImageSimilarity(testImageData[item.index], postProcessedImage);
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
        string similarityStatisticsFolder = Environment.GetEnvironmentVariable("Similarity_Statistics") ?? "Default_Similarity_Statistics_Path";

        if (!Directory.Exists(similarityStatisticsFolder))
        {
            Console.WriteLine($"Error: Folder path {similarityStatisticsFolder} does not exist.");
            return;
        }

        ExcelHelper.SaveSimilarityStatistics(
            htmTestSimilarities,
            Path.Combine(similarityStatisticsFolder, "HTM_Test_Similarity_Statistics.xlsx")
        );

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