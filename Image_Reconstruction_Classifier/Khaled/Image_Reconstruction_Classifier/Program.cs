using Image_Reconstruction_Classifier;
using ImageProcessing;
using System;
using System.IO;

/// <summary>
/// Main program for image reconstruction using HTM and KNN classifiers.
/// Thin entry point: builds a PipelineConfig from environment variables (set via
/// launchSettings.json for local runs) and invokes the shared Pipeline.RunFullPipeline.
/// The actual pipeline logic lives in Pipeline.cs so it can be reused by the cloud
/// experiment runner (MyExperiment) without duplicating any code.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        // Base data folders (kept identical to the original env-var layout).
        // For folders the original code resolved via relative-path fallback, we derive
        // them from the sample/binary folders' parents so behavior matches a local run.
        string trainingBinary = Env("Training_Image_Binary", "Training_Image_Binary");
        string trainingLoader = Env("Training_Image_Loader", "Training_Image_Loader");
        string trainingSpatial = Env("Training_Image_Spatial", "Training_Image_Spartial");
        string testImages = Env("Test_Images", "Test_Images");
        string testBinary = Env("Test_Image_Binary", "Test_Image_Binary");
        string testLoader = Env("Test_Image_Loader", "Test_Image_Loader");
        string testSpatial = Env("Test_Image_Spatial", "Test_Image_Spatial");
        string similarityStats = Env("Similarity_Statistics", "Default_Similarity_Statistics_Path");
        string combinedPng = Env("Test_Images_Reconstructed_Combined", "Test_Images_Reconstructed_Combined");

        // Training source PNGs: original code called ConvertImagesToBinary("Training_Image_Sample", ...)
        // with a literal relative folder. Derive it next to the training binary folder's parent
        // so a local run finds the same Data\Training\Training_Image_Sample folder.
        string trainingSample = Env("Training_Image_Sample",
            DeriveSibling(trainingBinary, "Training_Image_Sample"));

        var config = new PipelineConfig
        {
            TrainingImageSample = trainingSample,
            TrainingImageBinary = trainingBinary,
            TrainingImageLoader = trainingLoader,
            TrainingImageSpatial = trainingSpatial,

            TestImages = testImages,
            TestImageBinary = testBinary,
            TestImageLoader = testLoader,
            TestImageSpatial = testSpatial,

            ReconstructedBinaryHtm = Env("Test_Images_Reconstructed_Binary_HTM", "Test_Images_Reconstructed_Binary_HTM"),
            ReconstructedVectorHtm = Env("Test_Images_Reconstructed_Vector_HTM", "Test_Images_Reconstructed_Vector_HTM"),
            ReconstructedBinaryKnn = Env("Test_Images_Reconstructed_Binary_KNN", "Test_Images_Reconstructed_Binary_KNN"),
            ReconstructedVectorKnn = Env("Test_Images_Reconstructed_Vector_KNN", "Test_Images_Reconstructed_Vector_KNN"),
            ReconstructedBinaryCombined = Env("Test_Images_Reconstructed_Binary_Combined", "Test_Images_Reconstructed_Binary_Combined"),
            ReconstructedVectorCombined = Env("Test_Images_Reconstructed_Vector_Combined", "Test_Images_Reconstructed_Vector_Combined"),
            ReconstructedCombinedPng = combinedPng,

            SimilarityStatistics = similarityStats,
            TrainingDatasetSize = 10000
        };

        PipelineResult result = Pipeline.RunFullPipeline(config);

        Console.WriteLine();
        Console.WriteLine("===== RUN SUMMARY =====");
        Console.WriteLine($"Test images processed: {result.TestImagesProcessed}");
        Console.WriteLine($"HTM      avg vector/binary: {result.AvgHtmVectorSimilarity:F2}% / {result.AvgHtmBinarySimilarity:F2}%");
        Console.WriteLine($"KNN      avg vector/binary: {result.AvgKnnVectorSimilarity:F2}% / {result.AvgKnnBinarySimilarity:F2}%");
        Console.WriteLine($"Combined avg vector/binary: {result.AvgCombinedVectorSimilarity:F2}% / {result.AvgCombinedBinarySimilarity:F2}%");
        Console.WriteLine($"Duration: {result.Duration}");

        // Keep console open for interactive local debugging (unchanged from original).
        Console.ReadLine();
    }

    private static string Env(string name, string fallback)
    {
        string v = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(v) ? fallback : v;
    }

    /// <summary>
    /// Returns a folder with the given name that is a sibling of the supplied path
    /// (i.e. under the same parent directory). Used to locate the training sample
    /// PNG folder relative to the configured training binary folder.
    /// </summary>
    private static string DeriveSibling(string referencePath, string siblingFolderName)
    {
        try
        {
            string parent = Directory.GetParent(referencePath.TrimEnd(Path.DirectorySeparatorChar))?.FullName ?? "";
            return string.IsNullOrEmpty(parent) ? siblingFolderName : Path.Combine(parent, siblingFolderName);
        }
        catch
        {
            return siblingFolderName;
        }
    }
}