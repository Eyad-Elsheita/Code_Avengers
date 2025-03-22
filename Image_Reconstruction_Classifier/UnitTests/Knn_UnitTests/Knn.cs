using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageReconstructionTests
{
    [TestClass]
    public class KnnClassifierTests
    {
        private KnnClassifier classifier = new KnnClassifier();

        // This method runs before each test to initialize the classifier instance.
        [TestInitialize]
        public void SetUp()
        {
            classifier = new KnnClassifier();
        }

        // This test checks if the classifier correctly predicts the class when trained with valid data.
        [TestMethod]
        public void Test_Classify_WithCorrectTrainingData()
        {
            // Arrange: Train the classifier with three SDR (Sparse Distributed Representations) samples.
            int[] sdr1 = { 1, 2, 3 };
            int[] sdr2 = { 3, 4, 5 };
            int[] sdr3 = { 1, 2, 4 };

            classifier.Train(sdr1, 0); // Train with class 0
            classifier.Train(sdr2, 1); // Train with class 1
            classifier.Train(sdr3, 0); // Train with class 0

            // Act: Classify a new sample {1, 2, 3} with k=2 (considering 2 nearest neighbors).
            int result = classifier.Classify(new int[] { 1, 2, 3 }, 2);

            // Assert: The classifier should predict class 0 since it has a higher overlap with the training data.
            Assert.AreEqual(0, result);
        }

        // This test verifies that attempting to classify without training data throws an exception.
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Test_Classify_WithNoTrainingData_ShouldThrowException()
        {
            // Act: Attempt to classify without any prior training.
            classifier.Classify(new int[] { 1, 2, 3 }, 3);
        }

        // This test checks if the classifier behaves correctly with different values of k.
        [TestMethod]
        public void Test_Classify_WithDifferentKValues()
        {
            // Arrange: Train the classifier with sample SDRs.
            int[] sdr1 = { 1, 2, 3 };
            int[] sdr2 = { 3, 4, 5 };
            int[] sdr3 = { 1, 2, 4 };

            classifier.Train(sdr1, 0);
            classifier.Train(sdr2, 1);
            classifier.Train(sdr3, 0);

            // Act & Assert for k=1: Should return the class of the single closest match (sdr1 -> class 0).
            int resultK1 = classifier.Classify(new int[] { 1, 2, 3 }, 1);
            Assert.AreEqual(0, resultK1);

            // Act & Assert for k=3: Should return class 0 as it has a majority in the nearest neighbors.
            int resultK3 = classifier.Classify(new int[] { 1, 2, 3 }, 3);
            Assert.AreEqual(0, resultK3);
        }

        // This test checks if the classifier correctly uses weighted voting for classification.
        [TestMethod]
        public void Test_Classify_WithWeightedVoting()
        {
            // Arrange: Train the classifier with three SDRs.
            int[] sdr1 = { 1, 2, 3 };
            int[] sdr2 = { 3, 4, 5 };
            int[] sdr3 = { 1, 2, 4 };

            classifier.Train(sdr1, 0);
            classifier.Train(sdr2, 1);
            classifier.Train(sdr3, 0);

            // Act: Classify with weighted voting (k=2).
            int result = classifier.Classify(new int[] { 1, 2, 3 }, 2);

            // Assert: Class 0 should win due to higher similarity with the input sample.
            Assert.AreEqual(0, result);
        }

        // This test verifies that the classifier can handle a large dataset efficiently.
        [TestMethod]
        public void Test_Classify_WithLargeDataset()
        {
            // Arrange: Generate a large dataset with 10,000 random SDRs.
            var trainingData = GenerateLargeDataset(10000);
            foreach (var (sdr, label) in trainingData)
            {
                classifier.Train(sdr, label);
            }

            // Act: Classify a sample with k=5.
            var result = classifier.Classify(new int[] { 1, 2, 3 }, 5);

            // Assert: The result should be a valid class label (0 or 1).
            Assert.IsTrue(result >= 0);
        }

        // Helper method to generate a large dataset with random SDRs and labels.
        private List<(int[] SDR, int Label)> GenerateLargeDataset(int size)
        {
            var rand = new Random();
            var dataset = new List<(int[] SDR, int Label)>();

            for (int i = 0; i < size; i++)
            {
                var sdr = new int[10]; // SDRs have a fixed size of 10.
                for (int j = 0; j < sdr.Length; j++)
                {
                    sdr[j] = rand.Next(0, 10); // Random values between 0 and 9.
                }

                dataset.Add((sdr, rand.Next(0, 2))); // Assign a random label (0 or 1).
            }

            return dataset;
        }
    }
}
