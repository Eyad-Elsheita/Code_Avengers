using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi;
using NeoCortexApi.Entities;


namespace Image_Reconstruction_Classifier
{
    public class ImageSpatial
    {
        public static void SaveImagesinSpartialPooler ()
        {
            // Step 1: Define Define input and output directories

            // Get the input folder path and create the directory if it doesn't exist
            string inputFolder = Environment.GetEnvironmentVariable("Training_Image_Loader")!;

            // Ensure that you get the Path for the inputfolder if not replace it manually 
            if (string.IsNullOrEmpty(inputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths.");
                // Replace with your default path
                inputFolder = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Loader";

            }

            // Create the input directory if it doesn't exist
            DirectoryInfo inputDirectoryInfo = Directory.CreateDirectory(inputFolder);

            // Get the output folder path and 
            string outputFolder = Environment.GetEnvironmentVariable("Training_Image_Spatial")!;
            // Ensure that you get the path for the output folder 
            if (string.IsNullOrEmpty(outputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths.");
                // Replace with default path
                outputFolder = @"C:\try\Code_Avengers\Training_Image_Spartial";
            }

            //create the directory if it doesn't exist
            DirectoryInfo outputDirectoryInfo = Directory.CreateDirectory(outputFolder);


            // Step 2: Initialize the Spatial Pooler
            SpatialPooler spatialPooler = new SpatialPooler();
            Connections connections = new Connections();

            // Configure the existing HtmConfig instance
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

            // Step 3: Process each encoder output
            // Get all text files
            string[] encoderOutputFiles = Directory.GetFiles(inputFolder, "*.txt"); 
            foreach (string encoderOutputFile in encoderOutputFiles)
            {
                try
                {
                    // Get the file name without extension
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(encoderOutputFile);

                    // Step 3.1: Read the encoder output (bit vector) from the text file
                    int[] inputVector = File.ReadAllText(encoderOutputFile)
                        .Split(',')
                        .Select(int.Parse)
                        .ToArray();

                    // Step 3.2: Pass the input vector to the Spatial Pooler
                    int[] activeColumns = spatialPooler.Compute(inputVector, learn: true);

                    // Get the original file name and split it into code and label
                    string[] nameParts = fileNameWithoutExtension.Split('_');
                    string code = nameParts.Length > 0 ? nameParts[0] : "unknown";
                    string label = nameParts.Length > 1 ? nameParts[1] : "unknown";

                    // Save the Spatial Pooler's output to a new text file with the format code_label_spatial.txt
                    string spatialOutputFile = Path.Combine(outputFolder, $"{code}_{label}_spatial.txt");
                    File.WriteAllText(spatialOutputFile, string.Join(",", activeColumns));

                    Console.WriteLine($"Processed and saved spatial output for: {fileNameWithoutExtension}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {encoderOutputFile}: {ex.Message}");
                }
            }

            Console.WriteLine("Spartial Poler Completed.");
        }

        //public static void ProcessTestImagesSpatial()
        //{
        //    string inputFolder = Environment.GetEnvironmentVariable("Test_Image_Loader") ?? "Test_Image_Loader";
        //    string outputFolder = Environment.GetEnvironmentVariable("Test_Image_Spatial") ?? "Test_Image_Spatial";

        //    Directory.CreateDirectory(outputFolder);

        //    SpatialPooler spatialPooler = new SpatialPooler();
        //    Connections connections = new Connections();

        //    // Same SP configuration as before
        //    connections.HtmConfig.InputDimensions = new int[] { 784 };
        //    connections.HtmConfig.ColumnDimensions = new int[] { 2048 };
        //    connections.HtmConfig.PotentialRadius = 12;
        //    connections.HtmConfig.PotentialPct = 0.8;
        //    connections.HtmConfig.GlobalInhibition = true;
        //    connections.HtmConfig.LocalAreaDensity = 0.03;
        //    connections.HtmConfig.StimulusThreshold = 5;
        //    connections.HtmConfig.SynPermInactiveDec = 0.008;
        //    connections.HtmConfig.SynPermActiveInc = 0.05;
        //    connections.HtmConfig.SynPermConnected = 0.2;

        //    spatialPooler.Init(connections);

        //    string[] testFiles = Directory.GetFiles(inputFolder, "*.txt");
        //    foreach (var file in testFiles)
        //    {
        //        try
        //        {
        //            // ========== NEW VALIDATION CODE START ========
        //            string fileName = Path.GetFileNameWithoutExtension(file);
        //            string rawData = File.ReadAllText(file).Trim();

        //            // Check for empty files
        //            if (string.IsNullOrWhiteSpace(rawData))
        //            {
        //                Console.WriteLine($"⚠️ Empty SDR file: {fileName}");
        //                continue;
        //            }

        //            // Parse with validation
        //            int[] inputVector = rawData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        //                .Select(s => int.TryParse(s.Trim(), out int num) ? num : -1)
        //                .Where(n => n >= 0)
        //                .ToArray();

        //            // Validate SDR size
        //            if (inputVector.Length < 10)
        //            {
        //                Console.WriteLine($"⚠️ Invalid SDR in {fileName} (only {inputVector.Length} columns)");
        //                continue;
        //            }

        //            // Existing processing code
        //            int[] activeColumns = spatialPooler.Compute(inputVector, learn: false);

        //            // Additional output validation
        //            if (activeColumns.Length == 0)
        //            {
        //                Console.WriteLine($"⚠️ No active columns for: {fileName}");
        //                continue;
        //            }

        //            // Existing filename parsing and saving
        //            string[] nameParts = fileName.Split('_');
        //            string spatialFileName = $"{nameParts[0]}_{nameParts[1]}_spatial.txt";
        //            string outputFile = Path.Combine(outputFolder, spatialFileName);
        //            File.WriteAllText(outputFile, string.Join(",", activeColumns));

        //            Console.WriteLine($"Processed: {spatialFileName}");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"❌ Failed to process {Path.GetFileName(file)}: {ex.Message}");
        //        }

        //    }
        //    Console.WriteLine("Test images processed through Spatial Pooler.");
        //}

        public static void ProcessTestImagesSpatial()
        {
            string inputFolder = Environment.GetEnvironmentVariable("Test_Image_Loader") ?? "Test_Image_Loader";
            string outputFolder = Environment.GetEnvironmentVariable("Test_Image_Spatial") ?? "Test_Image_Spatial";

            Directory.CreateDirectory(outputFolder);

            SpatialPooler spatialPooler = new SpatialPooler();
            Connections connections = new Connections();

            // Spatial Pooler Configuration
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

            string[] testFiles = Directory.GetFiles(inputFolder, "*.txt");
            foreach (var file in testFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string rawData = File.ReadAllText(file).Trim();

                    //**** Added check for empty or whitespace-only file content ****
                    if (string.IsNullOrWhiteSpace(rawData)) 
                    {
                        Console.WriteLine($"⚠️ Empty SDR file: {fileName}");
                        continue; // Skip processing for empty files
                    }
                    

                    // Parse with validation
                    int[] inputVector = rawData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out int num) ? num : -1)
                        .Where(n => n >= 0)
                        .ToArray();

                    // Validate SDR size
                    if (inputVector.Length < 10)
                    {
                        Console.WriteLine($"⚠️ Invalid SDR in {fileName} (only {inputVector.Length} columns)");
                        continue;
                    }

                    // Process using Spatial Pooler
                    int[] activeColumns = spatialPooler.Compute(inputVector, learn: false);

                    // Additional output validation
                    if (activeColumns.Length == 0)
                    {
                        Console.WriteLine($"⚠️ No active columns for: {fileName}");
                        continue;
                    }

                    // Save spatial output
                    string[] nameParts = fileName.Split('_');
                    string spatialFileName = $"{nameParts[0]}_{nameParts[1]}_spatial.txt";
                    string outputFile = Path.Combine(outputFolder, spatialFileName);
                    File.WriteAllText(outputFile, string.Join(",", activeColumns));

                    Console.WriteLine($"Processed: {spatialFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to process {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            Console.WriteLine("Test images processed through Spatial Pooler.");
        }

    }
}