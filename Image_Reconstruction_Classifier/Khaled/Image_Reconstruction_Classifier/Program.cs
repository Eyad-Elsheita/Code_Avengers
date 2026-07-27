using Image_Reconstruction_Classifier;
using ImageProcessing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>
/// Main program for image reconstruction using HTM and KNN classifiers.
/// This program performs image preprocessing, training, testing, and post-processing.
/// </summary>
class Program
{
    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <param name="args">Command-line arguments (not used).</param>
    static void Main(string[] args)
    {
        // Start the stopwatch to measure total runtime
        Stopwatch stopwatch = Stopwatch.StartNew();

        // ==================================================
        // ======== PREPROCESSING & TRAINING SETUP ==========
        // ==================================================

        // Step 1: Convert the original images to binarized text files and load them.
        // The images from the "Training_Image_Sample" folder are binarized and stored in "Training_Image_Binary" folder.
        ImageProcessor.ConvertImagesToBinary("Training_Image_Sample", "Training_Image_Binary");
        Console.WriteLine("Image binarization completed.");

        // Get the folder path from an environment variable or use a default path.
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

        // Step 2: Vectorize training images and save them.
        // Vectorization converts image files to a one-dimensional array representation.
        string trainingLoaderFolder = Environment.GetEnvironmentVariable("Training_Image_Loader") ?? "Training_Image_Loader";
        Directory.CreateDirectory(trainingLoaderFolder);
        string[] trainingBinaryFiles = Directory.GetFiles(folderPath, "*.txt");

        foreach (var file in trainingBinaryFiles)
        {
            // Load image data and vectorize it.
            int[] vectorized = ImageLoader.LoadImage(file);
            string fileName = Path.GetFileNameWithoutExtension(file);
            string[] nameParts = fileName.Split('_');
            if (nameParts.Length < 3)
            {
                Console.WriteLine($"Invalid filename format: {fileName}");
                continue;
            }
            // Extract code and label from filename.
            string code = nameParts[0];
            string label = nameParts[1];
            string vectorizedFileName = $"{code}_{label}_vectorized.txt";
            // Save the vectorized image data.
            ImageLoader.SaveImageDataToFile(vectorized, Path.Combine(trainingLoaderFolder, vectorizedFileName));
        }
        Console.WriteLine($"Vectorized training images saved to {trainingLoaderFolder}");

        // Process images through Spatial Pooler.
        ImageSpatial.SaveImagesinSpartialPooler();
        Console.WriteLine("Spatial Pooler processing completed.");

        // ==================================================
        // ======== TRAINING PHASE: GROUP BY OBJECT TYPE =====
        // ==================================================

        // Get the folder for spatial processed images from environment variable or default value.
        string spatialFolder = Environment.GetEnvironmentVariable("Training_Image_Spatial") ?? "C:\\try\\Code_Avengers\\Training_Image_Spartial";
        if (!Directory.Exists(spatialFolder))
        {
            Console.WriteLine($"Error: Spatial folder not found: {spatialFolder}");
            return;
        }

        // Retrieve spatial files along with their indices (assumed to match imageData order).
        var spatialFilesWithIndex = Directory.GetFiles(spatialFolder, "*.txt")
            .Select((file, index) => new { file, index })
            .ToArray();

        // Dictionaries to store classifiers per object type (keys "0" to "9").
        Dictionary<string, MyHtmClassifier> htmClassifiers = new Dictionary<string, MyHtmClassifier>();
        Dictionary<string, KnnClassifier> knnClassifiers = new Dictionary<string, KnnClassifier>();

        // Loop through each object type (0-9) and train classifiers.
        for (int type = 0; type < 10; type++)
        {
            string typeStr = type.ToString();
            // Filter training files for the current object type based on filename.
            var filesForType = spatialFilesWithIndex
                .Where(x => Path.GetFileNameWithoutExtension(x.file).StartsWith(typeStr + "_"))
                .ToArray();

            if (!filesForType.Any())
            {
                Console.WriteLine($"No training files for object type {typeStr}");
                continue;
            }

            Console.WriteLine($"Training classifiers for object type {typeStr} with {filesForType.Length} images.");

            // Create new classifier instances for the current object type.
            var htmClassifierForType = new MyHtmClassifier();
            var knnClassifierForType = new KnnClassifier();

            // Train classifiers using the SDR data and corresponding original image data.
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

                    // Train both classifiers with the SDR and original image vector.
                    htmClassifierForType.Learn(idx, sdr, imageData[idx]);
                    knnClassifierForType.Train(sdr, idx);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {item.file}: {ex.Message}");
                }
            }

            // Store the trained classifiers in dictionaries keyed by object type.
            htmClassifiers[typeStr] = htmClassifierForType;
            knnClassifiers[typeStr] = knnClassifierForType;
            Console.WriteLine($"Training completed for object type {typeStr}");
        }
        Console.WriteLine("All object type training completed.");

        // ==================================================
        // ======== TESTING PHASE: GROUP BY OBJECT TYPE =====
        // ==================================================

        // Preprocess test images: binarize and vectorize similar to training images.
        string testImagesFolder = Environment.GetEnvironmentVariable("Test_Images") ?? "Test_Images";
        string testBinaryFolder = Environment.GetEnvironmentVariable("Test_Image_Binary") ?? "Test_Image_Binary";
        string testSpatialFolder = Environment.GetEnvironmentVariable("Test_Image_Spatial") ?? "Test_Image_Spatial";

        // Set environment variables for test image processing.
        Environment.SetEnvironmentVariable("Training_Image_Sample", testImagesFolder);
        Environment.SetEnvironmentVariable("Training_Image_Binary", testBinaryFolder);
        ImageProcessor.ConvertImagesToBinary("Test_Image_Sample", "Test_Image_Binary");

        // Vectorize test images.
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

        // Process test images through Spatial Pooler.
        Environment.SetEnvironmentVariable("Test_Image_Binary", testBinaryFolder);
        Environment.SetEnvironmentVariable("Test_Image_Spatial", testSpatialFolder);
        ImageSpatial.ProcessTestImagesSpatial();
        Console.WriteLine("Test dataset processing completed.");

        // Retrieve test spatial files with indices.
        var testSpatialFilesWithIndex = Directory.GetFiles(testSpatialFolder, "*.txt")
            .Select((file, index) => new { file, index })
            .ToArray();
        int[][] testImageData = ImageLoader.LoadImageData(testBinaryFolder, testSpatialFilesWithIndex.Length);

        // Define folders to save test reconstruction results for HTM and KNN.
        string htmTestBinaryFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Binary_HTM") ?? "Test_Images_Reconstructed_Binary_HTM";
        string htmTestVectorFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Vector_HTM") ?? "Test_Images_Reconstructed_Vector_HTM";
        string knnTestBinaryFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Binary_KNN") ?? "Test_Images_Reconstructed_Binary_KNN";
        string knnTestVectorFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Vector_KNN") ?? "Test_Images_Reconstructed_Vector_KNN";
        Directory.CreateDirectory(htmTestBinaryFolder);
        Directory.CreateDirectory(htmTestVectorFolder);
        Directory.CreateDirectory(knnTestBinaryFolder);
        Directory.CreateDirectory(knnTestVectorFolder);

        // Define folders to save combined reconstruction results.
        string combinedTestBinaryFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Binary_Combined") ?? "Test_Images_Reconstructed_Binary_Combined";
        string combinedTestVectorFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Vector_Combined") ?? "Test_Images_Reconstructed_Vector_Combined";
        Directory.CreateDirectory(combinedTestBinaryFolder);
        Directory.CreateDirectory(combinedTestVectorFolder);

        // Lists to hold similarity statistics for HTM, KNN, and combined results.
        List<SimilarityData> htmTestSimilarities = new List<SimilarityData>();
        List<SimilarityData> knnTestSimilarities = new List<SimilarityData>();
        List<SimilarityData> combinedTestSimilarities = new List<SimilarityData>();

        // Process test images for each object type (0-9).
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

            // Loop through each test file for current object type.
            foreach (var item in testFilesForType)
            {
                string originalFileName = Path.GetFileNameWithoutExtension(item.file);
                try
                {
                    // Load test SDR data.
                    int[] testSdr = File.ReadAllText(item.file)
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s) && int.TryParse(s, out _))
                        .Select(int.Parse)
                        .ToArray();

                    // -------------------------------
                    // HTM Test Reconstruction
                    // -------------------------------
                    int[] htmTestReconstructed = htmClassifierForType.GetPredictedInputValues(testSdr, 3);
                    SaveReconstructedImages(htmTestReconstructed, originalFileName, htmTestBinaryFolder, htmTestVectorFolder);
                    if (item.index >= testImageData.Length || testImageData[item.index] == null || testImageData[item.index].Length == 0)
                    {
                        Logger.LogError($"Skipping {originalFileName}: no valid test image data at index {item.index} (testImageData has {testImageData.Length} entries).");
                        continue;
                    }
                    double htmVectorSim = ImageSimilarity.CalculateCosineSimilarity(testImageData[item.index], htmTestReconstructed);
                    double htmBinarySim = CalculateBinarizedImageSimilarity(testImageData[item.index], htmTestReconstructed);
                    htmTestSimilarities.Add(new SimilarityData
                    {
                        PictureName = originalFileName,
                        VectorSimilarityPercentage = htmVectorSim * 100,
                        BinarySimilarityPercentage = htmBinarySim * 100
                    });

                    // -------------------------------
                    // KNN Test Reconstruction
                    // -------------------------------
                    int predictedLabel = knnClassifierForType.Classify(testSdr, 5);
                    int[] knnTestReconstructed = imageData[predictedLabel]; // use corresponding training image
                    SaveReconstructedImages(knnTestReconstructed, originalFileName, knnTestBinaryFolder, knnTestVectorFolder);
                    double knnVectorSim = ImageSimilarity.CalculateCosineSimilarity(testImageData[item.index], knnTestReconstructed);
                    double knnBinarySim = CalculateBinarizedImageSimilarity(testImageData[item.index], knnTestReconstructed);
                    knnTestSimilarities.Add(new SimilarityData
                    {
                        PictureName = originalFileName,
                        VectorSimilarityPercentage = knnVectorSim * 100,
                        BinarySimilarityPercentage = knnBinarySim * 100
                    });

                    // ---------------------------------------------------------
                    // Combined Reconstruction: using confidence weighting and Gaussian local voting
                    // ---------------------------------------------------------
                    // Assume the image dimensions are defined as (adjust if needed).
                    int width = 28;
                    int height = 28;

                    // Calculate total confidence from both methods.
                    double totalConfidence = htmVectorSim + knnVectorSim;
                    if (totalConfidence == 0)
                        totalConfidence = 1; // avoid division by zero

                    int[] combinedReconstructed = new int[htmTestReconstructed.Length];

                    // Combine each pixel from HTM and KNN using weighted confidence and local Gaussian vote.
                    for (int j = 0; j < htmTestReconstructed.Length; j++)
                    {
                        // Compute confidence weighted probability for the pixel.
                        double confWeightedProb = (htmVectorSim * htmTestReconstructed[j] + knnVectorSim * knnTestReconstructed[j]) / totalConfidence;

                        if (htmTestReconstructed[j] != knnTestReconstructed[j])
                        {
                            // Compute Gaussian weighted local votes from both reconstructions.
                            double htmLocalVote = GetGaussianWeightedLocalVote(htmTestReconstructed, j, width, height);
                            double knnLocalVote = GetGaussianWeightedLocalVote(knnTestReconstructed, j, width, height);
                            double localMajority = (htmLocalVote + knnLocalVote) / 2.0;

                            // Combine the confidence weighted probability with the Gaussian weighted local decision.
                            double combinedDecision = (confWeightedProb + localMajority) / 2.0;
                            combinedReconstructed[j] = combinedDecision >= 0.5 ? 1 : 0;
                        }
                        else
                        {
                            combinedReconstructed[j] = confWeightedProb >= 0.5 ? 1 : 0;
                        }
                    }

                    // --- Helper Method: GetPixelValueFromNeighborhood ---
                    // This method examines the 3x3 neighborhood around the given pixel (j)
                    // in a flat array representing an image of dimensions width x height.
                    static int GetPixelValueFromNeighborhood(int[] image, int pixelIndex, int width, int height)
                    {
                        int row = pixelIndex / width;
                        int col = pixelIndex % width;
                        int onesCount = 0;
                        int count = 0;

                        // Loop over a 3x3 window (handling boundaries)
                        for (int i = Math.Max(0, row - 1); i <= Math.Min(height - 1, row + 1); i++)
                        {
                            for (int j = Math.Max(0, col - 1); j <= Math.Min(width - 1, col + 1); j++)
                            {
                                onesCount += image[i * width + j];
                                count++;
                            }
                        }
                        // Return 1 if the majority of the window are 1's; otherwise 0.
                        return (onesCount > count / 2) ? 1 : 0;
                    }

                    /// <summary>
                    /// Calculates a Gaussian weighted average of the pixel values in a 3x3 neighborhood.
                    /// </summary>
                    /// <param name="image">The image array.</param>
                    /// <param name="pixelIndex">The target pixel index.</param>
                    /// <param name="width">Image width.</param>
                    /// <param name="height">Image height.</param>
                    /// <returns>A weighted average value between 0 and 1.</returns>
                    static double GetGaussianWeightedLocalVote(int[] image, int pixelIndex, int width, int height)
                    {
                        // Gaussian kernel for a 3x3 window (adjustable weights).
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

                        // Loop over the 3x3 window and apply the Gaussian kernel.
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
                        // Return the weighted average.
                        return weightedSum / totalWeight;
                    }

                    /// <summary>
                    /// Combines Gaussian weighted vote and majority vote to yield a binary decision.
                    /// </summary>
                    /// <param name="image">The image array.</param>
                    /// <param name="pixelIndex">Target pixel index.</param>
                    /// <param name="width">Image width.</param>
                    /// <param name="height">Image height.</param>
                    /// <returns>Binary value (0 or 1) after local vote.</returns>
                    static int GetCombinedLocalVote(int[] image, int pixelIndex, int width, int height)
                    {
                        // Get Gaussian weighted average (value between 0 and 1).
                        double gaussianVote = GetGaussianWeightedLocalVote(image, pixelIndex, width, height);
                        // Get strict majority vote (either 0 or 1).
                        int majorityVote = GetPixelValueFromNeighborhood(image, pixelIndex, width, height);

                        // Combine both values using predefined weights.
                        double weightGaussian = 0.7;
                        double weightMajority = 0.3;
                        double combinedScore = weightGaussian * gaussianVote + weightMajority * majorityVote;

                        // Return 0 or 1 based on threshold.
                        return combinedScore >= 0.5 ? 1 : 0;
                    }
                    // ==================================================
                    // ======== POST-PROCESSING: FILTER ORDER ==========
                    // ==================================================

                    // Step 1: Apply GetCombinedLocalVote to smooth edges and reduce noise.
                    int[] locallyVoted = new int[combinedReconstructed.Length];
                    for (int j = 0; j < combinedReconstructed.Length; j++)
                    {
                        locallyVoted[j] = GetCombinedLocalVote(combinedReconstructed, j, width, height);
                    }

                    // Step 2: Apply Median Filter to remove residual salt-and-pepper noise.
                    int[] postProcessedImage = ImageFilter.ApplyMedianFilter(locallyVoted, width, height);

                    // Save the combined binary image as PNG.
                    string combinedImagesFolder = Environment.GetEnvironmentVariable("Test_Images_Reconstructed_Combined") ?? "";
                    if (string.IsNullOrEmpty(combinedImagesFolder))
                    {
                        Console.WriteLine("Error: Environment variable 'Test_Images_Reconstructed_Combined' is not set.");
                        return;
                    }

                    {
                        int imageWidth = 28;  // Example width (adjust based on your dataset)
                        int imageHeight = 28; // Example height (adjust based on your dataset)
                        string outputCombinedFilePath = Path.Combine(combinedImagesFolder, originalFileName + "_Combined.png");

                        // Save the post-processed image as a PNG file.
                        BinaryToImageConverter.SaveBinaryAsPng(postProcessedImage, imageWidth, imageHeight, outputCombinedFilePath);
                    }

                    // Save the reconstructed images (binary and vectorized) using the post-processed image.
                    SaveReconstructedImages(postProcessedImage, originalFileName + "_Combined", combinedTestBinaryFolder, combinedTestVectorFolder);

                    // Calculate similarity using the post-processed image.
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
                    Logger.LogError($"Error processing test image {originalFileName}: {ex.Message}");
                    Logger.LogError(ex.StackTrace ?? "No stack trace available");
                }
            }
        }

        // Save test similarity statistics to Excel files.
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

        // Stop the stopwatch after all processing is complete
        stopwatch.Stop();
        Console.WriteLine("Total execution time: " + stopwatch.Elapsed);

        // Optional: wait for user input to keep the console open (for debugging)
        Console.ReadLine();
    }

    /// <summary>
    /// Saves reconstructed images in both vectorized and binary formats.
    /// </summary>
    /// <param name="image">The image data as a 1D integer array.</param>
    /// <param name="originalFileName">The base filename of the original image.</param>
    /// <param name="binaryFolder">Folder to save binary formatted images.</param>
    /// <param name="vectorFolder">Folder to save vectorized formatted images.</param>
    static void SaveReconstructedImages(int[] image, string originalFileName, string binaryFolder, string vectorFolder)
    {
        string vectorFile = Path.Combine(vectorFolder, $"{originalFileName}_Reconstructed_Vector.txt");
        string binaryFile = Path.Combine(binaryFolder, $"{originalFileName}_Reconstructed_Binary.txt");

        // Save the vectorized image data as comma-separated values.
        File.WriteAllText(vectorFile, string.Join(",", image));
        // Save the binarized image as a formatted string (matrix form).
        File.WriteAllText(binaryFile, ImageSimilarity.ConvertToBinaryMatrix(image, 28));
    }

    /// <summary>
    /// Calculates similarity between original and reconstructed binarized images.
    /// </summary>
    /// <param name="original">The original image data as a 1D integer array.</param>
    /// <param name="reconstructed">The reconstructed image data as a 1D integer array.</param>
    /// <returns>Similarity ratio (0 to 1) between the images.</returns>
    static double CalculateBinarizedImageSimilarity(int[] original, int[] reconstructed)
    {
        // Compare each pixel and compute the fraction of matching pixels.
        return original.Zip(reconstructed, (o, r) => o == r ? 1 : 0).Sum() / (double)original.Length;
    }
}
