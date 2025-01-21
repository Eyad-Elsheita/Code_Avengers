using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Image_Reconstruction_Classifier
{
    public class Classifier
    {
        public static void ImageClassifier()
        {
            // Step 1: Define Define input  directory

            // Get the input folder path and create the directory if it doesn't exist
            string inputFolder = Environment.GetEnvironmentVariable("Training_Image_Spartial")!;

            // Ensure that you get the Path for the inputfolder if not replace it manually 
            if (string.IsNullOrEmpty(inputFolder))
            {
                Console.WriteLine("Environment variables not set. Using default paths to get the Classifier Input.");
                // Replace with your default path
                inputFolder = @"D:\University\Software Engineering\se-cloud-2024-2025\MyProject\Image-Reconstruction-Project-\Training_Image_Spartial";

            }

            // Create the input directory if it doesn't exist
            DirectoryInfo inputDirectoryInfo = Directory.CreateDirectory(inputFolder);

            //Step 2: Read the Vectorized image  directory 



            // Step 3: Divide The Input Dataset Into Train and Test Sets 

        }
    }
}
