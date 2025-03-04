using System;
using System.Collections.Generic;
using System.Linq;

namespace Image_Reconstruction_Classifier
{
    // Our classifier interface remains unchanged.
    public interface IClassifier<TIN, TOUT>
    {
        void Learn(int key, TIN[] activeCells);
        TOUT[] GetPredictedInputValues(TIN[] predictiveCells, int k);
    }

    // Custom classifier for image reconstruction.
    // This implementation uses int for both input and output.
    public class MyHtmClassifier : IClassifier<int, int>
    {
        private class TrainingExample
        {
            public int Key { get; set; }
            public HashSet<int> SDR { get; set; }
            public int[] OriginalInput { get; set; }
        }

        private readonly List<TrainingExample> trainingExamples = new List<TrainingExample>();

        /// <summary>
        /// Learns by storing the SDR and the original input.
        /// </summary>
        public void Learn(int key, int[] activeCells)
        {
            // This overload does not include the original input.
            // For reconstruction we need the original input; you might throw an exception or simply ignore it.
            throw new NotImplementedException("Use the overload Learn(key, activeCells, originalInput) for image reconstruction.");
        }

        /// <summary>
        /// Overload to learn with the original input.
        /// </summary>
        public void Learn(int key, int[] activeCells, int[] originalInput)
        {
            if (activeCells == null || originalInput == null)
                throw new ArgumentNullException("Active cells and original input cannot be null.");

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
        public int[] GetPredictedInputValues(int[] predictiveCells, int k)
        {
            if (trainingExamples.Count == 0)
                throw new InvalidOperationException("No training examples available. Train the classifier first.");

            var predictiveSet = new HashSet<int>(predictiveCells);

            var scoredExamples = trainingExamples
                .Select(te => new
                {
                    Example = te,
                    Overlap = te.SDR.Intersect(predictiveSet).Count()
                })
                .OrderByDescending(x => x.Overlap)
                .Take(k)
                .ToList();

            // Ensure at least one example has original input data.
            int imageLength = scoredExamples[0].Example.OriginalInput?.Length ?? 0;
            if (imageLength == 0)
                throw new InvalidOperationException("Training examples do not contain original image data.");

            double[] pixelSums = new double[imageLength];
            foreach (var scored in scoredExamples)
            {
                // Get the overlap score as the weight - now squared to emphasize higher overlaps more
                double weight = Math.Pow(scored.Overlap, 2); // Squared weight

                for (int i = 0; i < imageLength; i++)
                {
                    // Weight each pixel by the squared overlap
                    pixelSums[i] += scored.Example.OriginalInput[i] * weight;
                }
            }

            int[] reconstructedImage = new int[imageLength];
            for (int i = 0; i < imageLength; i++)
            {
                double totalWeight = scoredExamples.Sum(s => s.Overlap);
                double avg = totalWeight > 0 ? pixelSums[i] / totalWeight : 0;
                reconstructedImage[i] = avg >= 0.5 ? 1 : 0;
            }

            return reconstructedImage;
        }
    }
}
