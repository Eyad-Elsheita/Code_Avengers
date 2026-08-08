using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImageProcessing;

namespace Image_Reconstruction_Classifier
{
    /// <summary>
    /// Holds every folder path and dataset size the reconstruction pipeline needs.
    /// Making these explicit (instead of reading ~15 environment variables at scattered
    /// points) lets the same pipeline run locally (Program.Main) or in the cloud
    /// experiment (Experiment.RunAsync) without relying on process-wide env-var state.
    /// </summary>
    public class PipelineConfig
    {
        // --- Training source + intermediate folders ---
        public string TrainingImageSample { get; set; } = "";   // input PNGs
        public string TrainingImageBinary { get; set; } = "";   // binarized .txt
        public string TrainingImageLoader { get; set; } = "";   // vectorized .txt
        public string TrainingImageSpatial { get; set; } = "";  // SDR .txt

        // --- Test source + intermediate folders ---
        public string TestImages { get; set; } = "";            // input PNGs
        public string TestImageBinary { get; set; } = "";       // binarized .txt
        public string TestImageLoader { get; set; } = "";       // vectorized .txt
        public string TestImageSpatial { get; set; } = "";      // SDR .txt

        // --- Reconstruction output folders ---
        public string ReconstructedBinaryHtm { get; set; } = "";
        public string ReconstructedVectorHtm { get; set; } = "";
        public string ReconstructedBinaryKnn { get; set; } = "";
        public string ReconstructedVectorKnn { get; set; } = "";
        public string ReconstructedBinaryCombined { get; set; } = "";
        public string ReconstructedVectorCombined { get; set; } = "";
        public string ReconstructedCombinedPng { get; set; } = ""; // final combined PNGs

        // --- Results ---
        public string SimilarityStatistics { get; set; } = "";  // Excel output folder

        // --- Dataset sizes ---
        public int TrainingDatasetSize { get; set; } = 10000;

        // --- Image dimensions ---
        public int ImageWidth { get; set; } = 28;
        public int ImageHeight { get; set; } = 28;
    }

    /// <summary>
    /// Summary of a completed pipeline run, used to populate the cloud ExperimentResult.
    /// </summary>
    public class PipelineResult
    {
        public TimeSpan Duration { get; set; }
        public int TestImagesProcessed { get; set; }
        public double AvgHtmVectorSimilarity { get; set; }
        public double AvgHtmBinarySimilarity { get; set; }
        public double AvgKnnVectorSimilarity { get; set; }
        public double AvgKnnBinarySimilarity { get; set; }
        public double AvgCombinedVectorSimilarity { get; set; }
        public double AvgCombinedBinarySimilarity { get; set; }
        public List<string> OutputExcelFiles { get; set; } = new List<string>();
    }

    /// <summary>
    /// The full HTM + KNN image reconstruction pipeline, extracted from Program.Main so it
    /// can be invoked both locally and from the cloud experiment runner.
    /// Logic is preserved exactly from the original Main(); the only structural change is
    /// that folder paths come from <see cref="PipelineConfig"/> and are pushed into the
    /// environment variables that ImageSpatial reads, deterministically at each stage
    /// (rather than being mutated as a hidden side effect mid-run).
    /// </summary>
    public static class Pipeline
    {
        public static PipelineResult RunFullPipeline(PipelineConfig config)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var result = new PipelineResult();

            // ==================================================
            // ======== PREPROCESSING & TRAINING SETUP ==========
            // ==================================================

            // Ensure all folders exist up front.
            EnsureDirectories(config);

            // Step 1: Binarize training source PNGs -> Training_Image_Binary.
            ImageProcessor.ConvertImagesToBinary(config.TrainingImageSample, config.TrainingImageBinary);
            Console.WriteLine("Image binarization completed.");

            if (!Directory.Exists(config.TrainingImageBinary))
            {
                Console.WriteLine($"Error: Folder path {config.TrainingImageBinary} does not exist.");
                return Finish(result, stopwatch);
            }

            int[][] imageData = ImageLoader.LoadImageData(config.TrainingImageBinary, config.TrainingDatasetSize);
            if (imageData.Length == 0)
            {
                Console.WriteLine("No images were loaded.");
                return Finish(result, stopwatch);
            }
            Console.WriteLine($"Loaded {imageData.Length} training images.");

            // Step 2: Vectorize training images -> Training_Image_Loader.
            string[] trainingBinaryFiles = Directory.GetFiles(config.TrainingImageBinary, "*.txt");
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
                ImageLoader.SaveImageDataToFile(vectorized, Path.Combine(config.TrainingImageLoader, vectorizedFileName));
            }
            Console.WriteLine($"Vectorized training images saved to {config.TrainingImageLoader}");

            // Process training images through Spatial Pooler.
            // ImageSpatial reads Training_Image_Loader (input) and Training_Image_Spatial (output) from env vars.
            Environment.SetEnvironmentVariable("Training_Image_Loader", config.TrainingImageLoader);
            Environment.SetEnvironmentVariable("Training_Image_Spatial", config.TrainingImageSpatial);
            ImageSpatial.SaveImagesinSpartialPooler();
            Console.WriteLine("Spatial Pooler processing completed.");

            // ==================================================
            // ======== TRAINING PHASE: GROUP BY OBJECT TYPE =====
            // ==================================================

            if (!Directory.Exists(config.TrainingImageSpatial))
            {
                Console.WriteLine($"Error: Spatial folder not found: {config.TrainingImageSpatial}");
                return Finish(result, stopwatch);
            }

            var spatialFilesWithIndex = Directory.GetFiles(config.TrainingImageSpatial, "*.txt")
                .Select((file, index) => new { file, index })
                .ToArray();

            Dictionary<string, MyHtmClassifier> htmClassifiers = new Dictionary<string, MyHtmClassifier>();
            Dictionary<string, KnnClassifier> knnClassifiers = new Dictionary<string, KnnClassifier>();

            for (int type = 0; type < 10; type++)
            {
                string typeStr = type.ToString();
                var filesForType = spatialFilesWithIndex
                    .Where(x => Path.GetFileNameWithoutExtension(x.file).StartsWith(typeStr + "_"))
                    .ToArray();

                if (!filesForType.Any())
                {
                    Console.WriteLine($"No training files for object type {typeStr}");
                    continue;
                }

                Console.WriteLine($"Training classifiers for object type {typeStr} with {filesForType.Length} images.");

                var htmClassifierForType = new MyHtmClassifier();
                var knnClassifierForType = new KnnClassifier();

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

                        htmClassifierForType.Learn(idx, sdr, imageData[idx]);
                        knnClassifierForType.Train(sdr, idx);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {item.file}: {ex.Message}");
                    }
                }

                htmClassifiers[typeStr] = htmClassifierForType;
                knnClassifiers[typeStr] = knnClassifierForType;
                Console.WriteLine($"Training completed for object type {typeStr}");
            }
            Console.WriteLine("All object type training completed.");

            // ==================================================
            // ======== TESTING PHASE: GROUP BY OBJECT TYPE =====
            // ==================================================

            // Binarize test source PNGs -> Test_Image_Binary.
            // NOTE: the original code reassigned the *training* env vars here as a hack so that
            // ImageProcessor (which falls back to Training_* env vars) would target test folders.
            // We now pass paths explicitly to ConvertImagesToBinary, so no env-var reassignment
            // is needed and training state is left intact.
            ImageProcessor.ConvertImagesToBinary(config.TestImages, config.TestImageBinary);

            // Vectorize test images -> Test_Image_Loader.
            string[] testBinaryFiles = Directory.GetFiles(config.TestImageBinary, "*.txt");
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
                ImageLoader.SaveImageDataToFile(vectorized, Path.Combine(config.TestImageLoader, vectorizedFileName));
            }
            Console.WriteLine($"Vectorized test images saved to {config.TestImageLoader}");

            // Process test images through Spatial Pooler.
            // ImageSpatial.ProcessTestImagesSpatial reads Test_Image_Loader (input) and Test_Image_Spatial (output).
            Environment.SetEnvironmentVariable("Test_Image_Loader", config.TestImageLoader);
            Environment.SetEnvironmentVariable("Test_Image_Spatial", config.TestImageSpatial);
            ImageSpatial.ProcessTestImagesSpatial();
            Console.WriteLine("Test dataset processing completed.");

            var testSpatialFilesWithIndex = Directory.GetFiles(config.TestImageSpatial, "*.txt")
                .Select((file, index) => new { file, index })
                .ToArray();
            int[][] testImageData = ImageLoader.LoadImageData(config.TestImageBinary, testSpatialFilesWithIndex.Length);

            List<SimilarityData> htmTestSimilarities = new List<SimilarityData>();
            List<SimilarityData> knnTestSimilarities = new List<SimilarityData>();
            List<SimilarityData> combinedTestSimilarities = new List<SimilarityData>();

            int width = config.ImageWidth;
            int height = config.ImageHeight;

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
                        SaveReconstructedImages(htmTestReconstructed, originalFileName, config.ReconstructedBinaryHtm, config.ReconstructedVectorHtm);
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

                        // KNN Test Reconstruction
                        int predictedLabel = knnClassifierForType.Classify(testSdr, 5);
                        int[] knnTestReconstructed = imageData[predictedLabel];
                        SaveReconstructedImages(knnTestReconstructed, originalFileName, config.ReconstructedBinaryKnn, config.ReconstructedVectorKnn);
                        double knnVectorSim = ImageSimilarity.CalculateCosineSimilarity(testImageData[item.index], knnTestReconstructed);
                        double knnBinarySim = CalculateBinarizedImageSimilarity(testImageData[item.index], knnTestReconstructed);
                        knnTestSimilarities.Add(new SimilarityData
                        {
                            PictureName = originalFileName,
                            VectorSimilarityPercentage = knnVectorSim * 100,
                            BinarySimilarityPercentage = knnBinarySim * 100
                        });

                        // Combined Reconstruction: confidence weighting + Gaussian local voting
                        double totalConfidence = htmVectorSim + knnVectorSim;
                        if (totalConfidence == 0)
                            totalConfidence = 1;

                        int[] combinedReconstructed = new int[htmTestReconstructed.Length];
                        for (int j = 0; j < htmTestReconstructed.Length; j++)
                        {
                            double confWeightedProb = (htmVectorSim * htmTestReconstructed[j] + knnVectorSim * knnTestReconstructed[j]) / totalConfidence;

                            if (htmTestReconstructed[j] != knnTestReconstructed[j])
                            {
                                double htmLocalVote = GetGaussianWeightedLocalVote(htmTestReconstructed, j, width, height);
                                double knnLocalVote = GetGaussianWeightedLocalVote(knnTestReconstructed, j, width, height);
                                double localMajority = (htmLocalVote + knnLocalVote) / 2.0;
                                double combinedDecision = (confWeightedProb + localMajority) / 2.0;
                                combinedReconstructed[j] = combinedDecision >= 0.5 ? 1 : 0;
                            }
                            else
                            {
                                combinedReconstructed[j] = confWeightedProb >= 0.5 ? 1 : 0;
                            }
                        }

                        // POST-PROCESSING: local vote smoothing then median filter
                        int[] locallyVoted = new int[combinedReconstructed.Length];
                        for (int j = 0; j < combinedReconstructed.Length; j++)
                        {
                            locallyVoted[j] = GetCombinedLocalVote(combinedReconstructed, j, width, height);
                        }
                        int[] postProcessedImage = ImageFilter.ApplyMedianFilter(locallyVoted, width, height);

                        // Save combined PNG.
                        string outputCombinedFilePath = Path.Combine(config.ReconstructedCombinedPng, originalFileName + "_Combined.png");
                        BinaryToImageConverter.SaveBinaryAsPng(postProcessedImage, width, height, outputCombinedFilePath);

                        SaveReconstructedImages(postProcessedImage, originalFileName + "_Combined", config.ReconstructedBinaryCombined, config.ReconstructedVectorCombined);

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

            // Save Excel statistics.
            if (!Directory.Exists(config.SimilarityStatistics))
            {
                Console.WriteLine($"Error: Folder path {config.SimilarityStatistics} does not exist.");
                return Finish(result, stopwatch);
            }

            string htmXlsx = Path.Combine(config.SimilarityStatistics, "HTM_Test_Similarity_Statistics.xlsx");
            string knnXlsx = Path.Combine(config.SimilarityStatistics, "KNN_Test_Similarity_Statistics.xlsx");
            string combinedXlsx = Path.Combine(config.SimilarityStatistics, "Combined_Test_Similarity_Statistics.xlsx");

            ExcelHelper.SaveSimilarityStatistics(htmTestSimilarities, htmXlsx);
            ExcelHelper.SaveSimilarityStatistics(knnTestSimilarities, knnXlsx);
            ExcelHelper.SaveSimilarityStatistics(combinedTestSimilarities, combinedXlsx);
            Console.WriteLine("Test dataset processing completed.");

            // Populate result summary.
            result.TestImagesProcessed = combinedTestSimilarities.Count;
            result.OutputExcelFiles.Add(htmXlsx);
            result.OutputExcelFiles.Add(knnXlsx);
            result.OutputExcelFiles.Add(combinedXlsx);
            if (htmTestSimilarities.Count > 0)
            {
                result.AvgHtmVectorSimilarity = htmTestSimilarities.Average(d => d.VectorSimilarityPercentage);
                result.AvgHtmBinarySimilarity = htmTestSimilarities.Average(d => d.BinarySimilarityPercentage);
            }
            if (knnTestSimilarities.Count > 0)
            {
                result.AvgKnnVectorSimilarity = knnTestSimilarities.Average(d => d.VectorSimilarityPercentage);
                result.AvgKnnBinarySimilarity = knnTestSimilarities.Average(d => d.BinarySimilarityPercentage);
            }
            if (combinedTestSimilarities.Count > 0)
            {
                result.AvgCombinedVectorSimilarity = combinedTestSimilarities.Average(d => d.VectorSimilarityPercentage);
                result.AvgCombinedBinarySimilarity = combinedTestSimilarities.Average(d => d.BinarySimilarityPercentage);
            }

            return Finish(result, stopwatch);
        }

        private static PipelineResult Finish(PipelineResult result, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            Console.WriteLine("Total execution time: " + stopwatch.Elapsed);
            return result;
        }

        private static void EnsureDirectories(PipelineConfig config)
        {
            foreach (var dir in new[]
            {
                config.TrainingImageSample, config.TrainingImageBinary, config.TrainingImageLoader, config.TrainingImageSpatial,
                config.TestImages, config.TestImageBinary, config.TestImageLoader, config.TestImageSpatial,
                config.ReconstructedBinaryHtm, config.ReconstructedVectorHtm,
                config.ReconstructedBinaryKnn, config.ReconstructedVectorKnn,
                config.ReconstructedBinaryCombined, config.ReconstructedVectorCombined,
                config.ReconstructedCombinedPng, config.SimilarityStatistics
            })
            {
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Saves reconstructed images in both vectorized and binary formats.
        /// </summary>
        private static void SaveReconstructedImages(int[] image, string originalFileName, string binaryFolder, string vectorFolder)
        {
            string vectorFile = Path.Combine(vectorFolder, $"{originalFileName}_Reconstructed_Vector.txt");
            string binaryFile = Path.Combine(binaryFolder, $"{originalFileName}_Reconstructed_Binary.txt");
            File.WriteAllText(vectorFile, string.Join(",", image));
            File.WriteAllText(binaryFile, ImageSimilarity.ConvertToBinaryMatrix(image, 28));
        }

        /// <summary>
        /// Calculates similarity between original and reconstructed binarized images.
        /// </summary>
        private static double CalculateBinarizedImageSimilarity(int[] original, int[] reconstructed)
        {
            return original.Zip(reconstructed, (o, r) => o == r ? 1 : 0).Sum() / (double)original.Length;
        }

        /// <summary>
        /// Majority vote over a 3x3 neighborhood.
        /// </summary>
        private static int GetPixelValueFromNeighborhood(int[] image, int pixelIndex, int width, int height)
        {
            int row = pixelIndex / width;
            int col = pixelIndex % width;
            int onesCount = 0;
            int count = 0;
            for (int i = Math.Max(0, row - 1); i <= Math.Min(height - 1, row + 1); i++)
            {
                for (int j = Math.Max(0, col - 1); j <= Math.Min(width - 1, col + 1); j++)
                {
                    onesCount += image[i * width + j];
                    count++;
                }
            }
            return (onesCount > count / 2) ? 1 : 0;
        }

        /// <summary>
        /// Gaussian weighted average of a 3x3 neighborhood.
        /// </summary>
        private static double GetGaussianWeightedLocalVote(int[] image, int pixelIndex, int width, int height)
        {
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
            return weightedSum / totalWeight;
        }

        /// <summary>
        /// Combines Gaussian weighted vote and majority vote into a binary decision.
        /// </summary>
        private static int GetCombinedLocalVote(int[] image, int pixelIndex, int width, int height)
        {
            double gaussianVote = GetGaussianWeightedLocalVote(image, pixelIndex, width, height);
            int majorityVote = GetPixelValueFromNeighborhood(image, pixelIndex, width, height);
            double weightGaussian = 0.7;
            double weightMajority = 0.3;
            double combinedScore = weightGaussian * gaussianVote + weightMajority * majorityVote;
            return combinedScore >= 0.5 ? 1 : 0;
        }
    }
}
