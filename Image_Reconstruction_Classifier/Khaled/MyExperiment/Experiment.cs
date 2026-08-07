using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCloudProject.Common;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Image_Reconstruction_Classifier;

namespace MyExperiment
{
    /// <summary>
    /// Implements the cloud experiment. Wraps the existing HTM+KNN reconstruction pipeline
    /// (Pipeline.RunFullPipeline) so it can be triggered by a queue message.
    ///
    /// Expected input: a .zip file (downloaded by the storage provider to a local path,
    /// passed in as <paramref name="inputData"/>) containing two folders of PNG images:
    ///     Training_Image_Sample/   - training source images
    ///     Test_Images/             - test source images
    /// Everything else (binary/loader/spatial intermediates, reconstruction outputs,
    /// Excel stats) is generated under a per-run working directory.
    /// </summary>
    public class Experiment : IExperiment
    {
        private readonly IStorageProvider storageProvider;
        private readonly ILogger logger;
        private readonly MyConfig config;

        public Experiment(IConfigurationSection configSection, IStorageProvider storageProvider, ILogger log)
        {
            this.storageProvider = storageProvider;
            this.logger = log;

            config = new MyConfig();
            configSection.Bind(config);
        }

        public Task<IExperimentResult> RunAsync(string inputData)
        {
            // RowKey: unique per run so multiple experiments don't overwrite each other in the table.
            var res = new ExperimentResult(this.config.GroupId, Guid.NewGuid().ToString())
            {
                ExperimentId = Guid.NewGuid().ToString(),
                Name = "Image Reconstruction Classifier (HTM + KNN)",
                Description = "Reconstructs 28x28 grayscale clothing images via HTM and KNN classifiers and reports similarity statistics.",
                InputFileUrl = inputData,
                StartTimeUtc = DateTime.UtcNow
            };

            logger?.LogInformation($"Experiment start. Input file: {inputData}");

            // Per-run working directory (isolated so concurrent/subsequent runs don't collide).
            string workRoot = Path.Combine(Path.GetTempPath(), "ImageReconRun_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workRoot);

            try
            {
                // 1. Extract the input zip into the working directory.
                string extractRoot = Path.Combine(workRoot, "input");
                Directory.CreateDirectory(extractRoot);
                logger?.LogInformation($"Extracting input archive to {extractRoot}");
                ZipFile.ExtractToDirectory(inputData, extractRoot, overwriteFiles: true);

                // 2. Locate the two required source folders inside the archive.
                //    We search recursively so it works whether the zip has them at the root
                //    or nested one level down.
                string trainingSample = FindFolder(extractRoot, "Training_Image_Sample");
                string testImages = FindFolder(extractRoot, "Test_Images");

                if (trainingSample == null)
                    throw new DirectoryNotFoundException("Input archive is missing the 'Training_Image_Sample' folder.");
                if (testImages == null)
                    throw new DirectoryNotFoundException("Input archive is missing the 'Test_Images' folder.");

                logger?.LogInformation($"Training source: {trainingSample}");
                logger?.LogInformation($"Test source: {testImages}");

                // 3. Build a PipelineConfig with all intermediate + output folders under workRoot.
                string outRoot = Path.Combine(workRoot, "output");
                var pipelineConfig = new PipelineConfig
                {
                    TrainingImageSample = trainingSample,
                    TrainingImageBinary = Path.Combine(outRoot, "Training_Image_Binary"),
                    TrainingImageLoader = Path.Combine(outRoot, "Training_Image_Loader"),
                    TrainingImageSpatial = Path.Combine(outRoot, "Training_Image_Spatial"),

                    TestImages = testImages,
                    TestImageBinary = Path.Combine(outRoot, "Test_Image_Binary"),
                    TestImageLoader = Path.Combine(outRoot, "Test_Image_Loader"),
                    TestImageSpatial = Path.Combine(outRoot, "Test_Image_Spatial"),

                    ReconstructedBinaryHtm = Path.Combine(outRoot, "Reconstructed_Binary_HTM"),
                    ReconstructedVectorHtm = Path.Combine(outRoot, "Reconstructed_Vector_HTM"),
                    ReconstructedBinaryKnn = Path.Combine(outRoot, "Reconstructed_Binary_KNN"),
                    ReconstructedVectorKnn = Path.Combine(outRoot, "Reconstructed_Vector_KNN"),
                    ReconstructedBinaryCombined = Path.Combine(outRoot, "Reconstructed_Binary_Combined"),
                    ReconstructedVectorCombined = Path.Combine(outRoot, "Reconstructed_Vector_Combined"),
                    ReconstructedCombinedPng = Path.Combine(outRoot, "Reconstructed_Combined_PNG"),

                    SimilarityStatistics = Path.Combine(outRoot, "Similarity_Statistics"),

                    // Demo default: cap training set. The full local run uses 10000; for a
                    // cloud demo the input archive is expected to be a smaller subset, but this
                    // cap also protects against an accidentally huge archive.
                    TrainingDatasetSize = 10000
                };

                // 4. Run the actual pipeline (same code path as the verified local run).
                logger?.LogInformation("Running reconstruction pipeline...");
                PipelineResult pipelineResult = Pipeline.RunFullPipeline(pipelineConfig);

                // 5. Map pipeline output onto the cloud ExperimentResult.
                res.EndTimeUtc = DateTime.UtcNow;
                res.Duration = pipelineResult.Duration;
                res.DurationSec = (long)pipelineResult.Duration.TotalSeconds;
                // Use the combined-classifier average vector similarity as the headline accuracy.
                res.Accuracy = (float)pipelineResult.AvgCombinedVectorSimilarity;
                res.OutputFiles = pipelineResult.OutputExcelFiles?.ToArray() ?? Array.Empty<string>();

                logger?.LogInformation(
                    $"Experiment done. Processed {pipelineResult.TestImagesProcessed} test images. " +
                    $"Combined avg vector/binary: {pipelineResult.AvgCombinedVectorSimilarity:F2}% / " +
                    $"{pipelineResult.AvgCombinedBinarySimilarity:F2}%. Duration: {pipelineResult.Duration}.");

                return Task.FromResult<IExperimentResult>(res);
            }
            catch (Exception ex)
            {
                res.EndTimeUtc = DateTime.UtcNow;
                if (res.StartTimeUtc.HasValue)
                {
                    res.Duration = res.EndTimeUtc.Value - res.StartTimeUtc.Value;
                    res.DurationSec = (long)res.Duration.TotalSeconds;
                }
                logger?.LogError(ex, $"Experiment failed: {ex.Message}");
                throw;
            }
            // NOTE: workRoot is intentionally left on disk for now so results can be
            // inspected during the integration-test phase. Cleanup can be added later once the
            // end-to-end flow is confirmed.
        }

        /// <summary>
        /// Finds the first directory with the given name at or under <paramref name="root"/>.
        /// Returns null if not found.
        /// </summary>
        private static string FindFolder(string root, string folderName)
        {
            if (string.Equals(Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar)), folderName, StringComparison.OrdinalIgnoreCase))
                return root;

            return Directory
                .EnumerateDirectories(root, folderName, SearchOption.AllDirectories)
                .FirstOrDefault();
        }
    }
}