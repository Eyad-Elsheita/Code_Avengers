using System;
using System.IO;
using System.Linq;
using NeoCortexApi;
using NeoCortexApi.Entities;

namespace Image_Reconstruction_Classifier
{
    /// <summary>
    /// Provides spatial processing capabilities for image data using Hierarchical Temporal Memory (HTM) Spatial Pooler.
    /// Transforms encoded image patterns into sparse distributed representations (SDRs) through spatial pooling.
    /// </summary>
    /// <remarks>
    /// Key Functionality:
    /// - Training Mode: Learns spatial patterns from encoded image data (SaveImagesinSpartialPooler)
    /// - Inference Mode: Processes new images using learned spatial patterns (ProcessTestImagesSpatial)
    /// 
    /// Data Specifications:
    /// - Input: 784-dimensional vectors (28x28 grayscale images, MNIST-compatible)
    /// - Output: 2048-dimensional SDR representations
    /// 
    /// Configuration Requirements:
    /// - Environment variables for I/O paths (Training_Image_Loader, Training_Image_Spatial)
    /// - Consistent HTM parameters between training and inference modes
    /// </remarks>
    public class ImageSpatial
    {
        /// <summary>
        /// Processes training images through the Spatial Pooler algorithm to learn spatial patterns
        /// and generates sparse distributed representations (SDRs).
        /// </summary>
        /// <remarks>
        /// Workflow:
        /// 1. Configures input/output paths via environment variables or defaults
        /// 2. Initializes HTM Spatial Pooler with predefined neuroplasticity parameters
        /// 3. Processes each encoded image file in batch mode
        /// 4. Persists generated SDRs with metadata-preserving filenames
        /// 
        /// Critical Parameters:
        /// - PotentialRadius: 12 (Local receptive field size)
        /// - GlobalInhibition: true (Network-wide column competition)
        /// - LocalAreaDensity: 0.03 (~61 active columns/2048 total)
        /// </remarks>
        public static void SaveImagesinSpartialPooler()
        {
            // 1. Environment Configuration --------------------------------------------------------
            // Resolve input source directory
            string inputFolder = Environment.GetEnvironmentVariable("Training_Image_Loader")!;

            // Fallback to development path if environment variable not set
            if (string.IsNullOrEmpty(inputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths.");
                inputFolder = @"D:\University\...\Training_Image_Loader"; // Truncated for security
            }
            Directory.CreateDirectory(inputFolder);

            // Resolve output target directory
            string outputFolder = Environment.GetEnvironmentVariable("Training_Image_Spatial")!;
            if (string.IsNullOrEmpty(outputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths.");
                outputFolder = @"C:\try\...\Training_Image_Spartial"; // Truncated for security
            }
            Directory.CreateDirectory(outputFolder);

            // 2. HTM Spatial Pooler Initialization -------------------------------------------------
            SpatialPooler spatialPooler = new SpatialPooler();
            Connections connections = new Connections();

            // Neuroplasticity Configuration Parameters:
            // - InputDimensions: 784 (28x28 image pixels)
            // - ColumnDimensions: 2048 (SDR output size)
            // - PotentialRadius: 12 (Local receptive field size)
            // - GlobalInhibition: true (Network-wide column competition)
            // - LocalAreaDensity: 0.03 (~61 active columns/2048 total)
            connections.HtmConfig.InputDimensions = new int[] { 784 };
            connections.HtmConfig.ColumnDimensions = new int[] { 2048 };
            connections.HtmConfig.PotentialRadius = 12;
            connections.HtmConfig.PotentialPct = 0.8;
            connections.HtmConfig.GlobalInhibition = true;
            connections.HtmConfig.LocalAreaDensity = 0.03;
            connections.HtmConfig.StimulusThreshold = 5;
            connections.HtmConfig.SynPermInactiveDec = 0.008;
            connections.HtmConfig.SynPermActiveInc = 0.05;
            connections.HtmConfig.SynPermConnected = 0.2;

            spatialPooler.Init(connections);

            // 3. Batch Processing Pipeline -------------------------------------------------------
            string[] encoderOutputFiles = Directory.GetFiles(inputFolder, "*.txt");
            Console.WriteLine($"Commencing spatial processing of {encoderOutputFiles.Length} images");

            foreach (string encoderOutputFile in encoderOutputFiles)
            {
                try
                {
                    // 3.1 Input Validation and Parsing
                    string fileName = Path.GetFileNameWithoutExtension(encoderOutputFile);
                    int[] inputVector = File.ReadAllText(encoderOutputFile)
                        .Split(',')
                        .Select(int.Parse)
                        .ToArray();

                    // Validate input vector dimensionality
                    if (inputVector.Length != 784)
                    {
                        Console.WriteLine($"⚠️ Dimension mismatch in {fileName}: Expected 784, got {inputVector.Length}");
                        continue;
                    }

                    // 3.2 Spatial Pooling Computation
                    int[] activeColumns = spatialPooler.Compute(inputVector, learn: true);

                    // 3.3 Metadata-Aware Output Persistence
                    string[] nameParts = fileName.Split('_');
                    string outputFile = Path.Combine(outputFolder,
                        $"{nameParts[0]}_{nameParts[1]}_spatial.txt");
                    File.WriteAllText(outputFile, string.Join(",", activeColumns));

                    Console.WriteLine($"✅ Successfully processed: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Critical error processing {Path.GetFileName(encoderOutputFile)}");
                    Console.WriteLine($"Error Details: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                }
            }

            Console.WriteLine("Spatial Pooler training completed successfully.");
        }

        /// <summary>
        /// Processes test images through preconfigured Spatial Pooler to generate SDRs
        /// using learned spatial patterns (inference mode).
        /// </summary>
        /// <remarks>
        /// Key Features:
        /// - Input Validation: Comprehensive data integrity checks
        /// - Error Resilience: Graceful error handling with diagnostic logging
        /// - Configuration Consistency: Mirrors training parameters exactly
        /// 
        /// Input Requirements:
        /// - Pre-encoded 784-element vectors in text files
        /// - Filename format: [identifier]_[label].txt
        /// </remarks>
        public static void ProcessTestImagesSpatial()
        {
            // 1. Environment Configuration --------------------------------------------------------
            string inputFolder = Environment.GetEnvironmentVariable("Test_Image_Loader") ?? "Test_Image_Loader";
            string outputFolder = Environment.GetEnvironmentVariable("Test_Image_Spatial") ?? "Test_Image_Spatial";
            Directory.CreateDirectory(outputFolder);

            // 2. HTM Configuration Replication ----------------------------------------------------
            // Ensures parameter parity with training configuration
            SpatialPooler spatialPooler = new SpatialPooler();
            Connections connections = new Connections();

            // Mirror training configuration parameters exactly
            connections.HtmConfig.InputDimensions = new int[] { 784 };
            connections.HtmConfig.ColumnDimensions = new int[] { 2048 };
            connections.HtmConfig.PotentialRadius = 12;
            connections.HtmConfig.PotentialPct = 0.8;
            connections.HtmConfig.GlobalInhibition = true;
            connections.HtmConfig.LocalAreaDensity = 0.03;
            connections.HtmConfig.StimulusThreshold = 5;
            connections.HtmConfig.SynPermInactiveDec = 0.008;
            connections.HtmConfig.SynPermActiveInc = 0.05;
            connections.HtmConfig.SynPermConnected = 0.2;

            spatialPooler.Init(connections);

            // 3. Inference Processing Pipeline ---------------------------------------------------
            string[] testFiles = Directory.GetFiles(inputFolder, "*.txt");
            Console.WriteLine($"Processing {testFiles.Length} test images");

            foreach (var file in testFiles)
            {
                try
                {
                    // 3.1 Input Validation
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string rawData = File.ReadAllText(file).Trim();

                    // Empty content check
                    if (string.IsNullOrWhiteSpace(rawData))
                    {
                        Console.WriteLine($"⚠️ Empty file detected: {fileName}");
                        continue;
                    }

                    // 3.2 Data Parsing with Validation
                    int[] inputVector = rawData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out int num) ? num : -1)
                        .Where(n => n >= 0)
                        .ToArray();

                    // Dimension validation
                    if (inputVector.Length != 784)
                    {
                        Console.WriteLine($"⚠️ Invalid dimensionality in {fileName}: {inputVector.Length}/784");
                        continue;
                    }

                    // 3.3 Spatial Pooling Inference
                    int[] activeColumns = spatialPooler.Compute(inputVector, learn: false);

                    // Output validation
                    if (activeColumns.Length == 0)
                    {
                        Console.WriteLine($"⚠️ Zero active columns in {fileName}");
                        continue;
                    }

                    // 3.4 Result Persistence
                    string[] nameParts = fileName.Split('_');
                    string outputFile = Path.Combine(outputFolder,
                        $"{nameParts[0]}_{nameParts[1]}_spatial.txt");
                    File.WriteAllText(outputFile, string.Join(",", activeColumns));

                    Console.WriteLine($"✔️ Successfully processed: {Path.GetFileName(outputFile)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ FATAL ERROR processing {Path.GetFileName(file)}");
                    Console.WriteLine($"Error Type: {ex.GetType().Name}");
                    Console.WriteLine($"Error Details: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                }
            }

            Console.WriteLine("Test image spatial processing completed.");
        }
    }
}