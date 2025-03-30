using System;
using System.Collections.Generic;
using System.Linq;

namespace Image_Reconstruction_Classifier
{
    // Generic classifier interface for learning and prediction operations.
    public interface IClassifier<TIN, TOUT>
    {
        // Learns from a given key and an array of active cells.
        void Learn(int key, TIN[] activeCells);

        // Given predictive cells and the number k, returns an array of predicted input values.
        TOUT[] GetPredictedInputValues(TIN[] predictiveCells, int k);
    }

    // Custom classifier for image reconstruction.
    // This implementation uses int for both input and output.
    public class MyHtmClassifier : IClassifier<int, int>
    {
        // Represents a single training example containing the key, the SDR, and the original input.
        public class TrainingExample
        {
            public int Key { get; set; }
            // A set representing the Sparse Distributed Representation (SDR) of active cells.
            public required HashSet<int> SDR { get; set; }
            // The original input used for image reconstruction.
            public required int[] OriginalInput { get; set; }
        }

        // Stores all training examples for the classifier.
        private readonly List<TrainingExample> trainingExamples = new List<TrainingExample>();

        /// <summary>
        /// Learns by storing the SDR.
        /// This overload does not include the original input.
        /// For reconstruction, the original input is required.
        /// </summary>
        public void Learn(int key, int[] activeCells)
        {
            // This overload does not include the original input.
            // For reconstruction we need the original input; you might throw an exception or simply ignore it.
            throw new NotImplementedException("Use the overload Learn(key, activeCells, originalInput) for image reconstruction.");
        }

        /// <summary>
        /// Overload to learn with the original input.
        /// Stores both the SDR and the original image data.
        /// </summary>
        /// <param name="key">Unique identifier for the training example.</param>
        /// <param name="activeCells">Array of active cell indices representing the SDR.</param>
        /// <param name="originalInput">The original input image as an array of pixel values.</param>
        public void Learn(int key, int[] activeCells, int[] originalInput)
        {
            // Validate input: ensure that activeCells and originalInput are not null.
            if (activeCells == null || originalInput == null)
                throw new ArgumentNullException("Active cells and original input cannot be null.");

            // Add a new training example with the provided key, SDR, and original image data.
            trainingExamples.Add(new TrainingExample
            {
                Key = key,
                SDR = new HashSet<int>(activeCells),
                OriginalInput = originalInput
            });
        }

        /// <summary>
        /// Given a predictive SDR, finds the k training examples with the highest overlap and uses them to reconstruct the image.
        /// </summary>
        /// <param name="predictiveCells">An array representing the predictive SDR.</param>
        /// <param name="k">The number of top matching training examples to consider.</param>
        /// <returns>An array representing the reconstructed image as binary pixel values.</returns>
        public int[] GetPredictedInputValues(int[] predictiveCells, int k)
        {
            // Ensure there are training examples available before attempting reconstruction.
            if (trainingExamples.Count == 0)
                throw new InvalidOperationException("No training examples available. Train the classifier first.");

            // Create a set from predictiveCells for faster intersection operations.
            var predictiveSet = new HashSet<int>(predictiveCells);

            // Compute the overlap between the predictive set and each training example's SDR.
            // Order the examples by the count of overlapping active cells in descending order and take the top k.
            var scoredExamples = trainingExamples
                .Select(te => new
                {
                    Example = te,
                    Overlap = te.SDR.Intersect(predictiveSet).Count()
                })
                .OrderByDescending(x => x.Overlap)
                .Take(k)
                .ToList();

            // Get the length of the original input image from the first example.
            // Ensure that at least one training example contains original image data.
            int imageLength = scoredExamples[0].Example.OriginalInput?.Length ?? 0;
            if (imageLength == 0)
                throw new InvalidOperationException("Training examples do not contain original image data.");

            // Initialize an array to accumulate weighted pixel sums.
            double[] pixelSums = new double[imageLength];
            foreach (var scored in scoredExamples)
            {
                // Compute the weight for the current example based on the overlap.
                // The weight is squared to emphasize higher overlaps.
                double weight = Math.Pow(scored.Overlap, 2); // Squared weight

                // For each pixel in the original input, add the weighted pixel value to the accumulator.
                for (int i = 0; i < imageLength; i++)
                {
                    pixelSums[i] += scored.Example.OriginalInput[i] * weight;
                }
            }

            // Reconstruct the image by converting the accumulated sums into binary pixel values.
            int[] reconstructedImage = new int[imageLength];
            for (int i = 0; i < imageLength; i++)
            {
                // Calculate the total weight from the scored examples.
                double totalWeight = scoredExamples.Sum(s => s.Overlap);
                // Compute the average value for the pixel; if no weight, default to 0.
                double avg = totalWeight > 0 ? pixelSums[i] / totalWeight : 0;
                // Convert the average to binary: pixel value is 1 if avg is at least 0.5, else 0.
                reconstructedImage[i] = avg >= 0.5 ? 1 : 0;
            }

            // Return the reconstructed binary image.
            return reconstructedImage;
        }
    }
}
