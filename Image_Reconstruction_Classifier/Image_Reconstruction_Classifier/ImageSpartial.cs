using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi;
using NeoCortexApi.Entities;


namespace Image_Reconstruction_Classifier
{
    public class ImageSpartial
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
            string outputFolder = Environment.GetEnvironmentVariable("Training_Image_Spartial")!;
            // Ensure that you get the path for the output folder 
            if (string.IsNullOrEmpty(outputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths.");
                // Replace with default path
                outputFolder = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Spartial";
            }

            //create the directory if it doesn't exist
            DirectoryInfo outputDirectoryInfo = Directory.CreateDirectory(outputFolder);


            // Step 2: Initialize the Spatial Pooler
            SpatialPooler spatialPooler = new SpatialPooler();
            Connections connections = new Connections();

            // Configure the existing HtmConfig instance
            connections.HtmConfig.InputDimensions = new int[] { 784 }; // Adjust to match encoder output length
            connections.HtmConfig.ColumnDimensions = new int[] { 1024 }; // Number of columns in the Spatial Pooler
            connections.HtmConfig.LocalAreaDensity = 0.02;
            connections.HtmConfig.NumActiveColumnsPerInhArea = 40;
            connections.HtmConfig.StimulusThreshold = 10;
            connections.HtmConfig.SynPermConnected = 0.2;
            connections.HtmConfig.SynPermActiveInc = 0.03;
            connections.HtmConfig.SynPermInactiveDec = 0.01;

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

                    // Step 3.3: Save the Spatial Pooler's output to a new text file
                    string spatialOutputFile = Path.Combine(outputFolder, fileNameWithoutExtension + "_spatial.txt");
                    File.WriteAllText(spatialOutputFile, string.Join(",", activeColumns));

                    Console.WriteLine($"Processed and saved output for: {fileNameWithoutExtension}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {encoderOutputFile}: {ex.Message}");
                }
            }

            Console.WriteLine("Spartial Poler Completed.");
        }
    }
}
