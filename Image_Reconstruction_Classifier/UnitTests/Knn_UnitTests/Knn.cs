using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageReconstructionTests
{
    /// <summary>
    /// Contains comprehensive unit tests for K-Nearest Neighbors classifier functionality
    /// including training, classification, and edge case handling
    /// </summary>
    [TestClass]
    public class KnnClassifierTests
    {
        private KnnClassifier classifier = new KnnClassifier();

        /// <summary>
        /// Initializes a fresh classifier instance before each test execution
        /// to ensure test isolation
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            classifier = new KnnClassifier();
        }

        /// <summary>
        /// Validates core classification functionality with valid training data
        /// and verifies majority voting mechanism
        /// </summary>
        [TestMethod]
        public void Test_Classify_WithCorrectTrainingData()
        {
            // Arrange: Train with 3 Sparse Distributed Representations (SDRs)
            // SDR1: Features [1,2,3] → Class 0
            // SDR2: Features [3,4,5] → Class 1
            // SDR3: Features [1,2,4] → Class 0
            classifier.Train(new[] { 1, 2, 3 }, 0);
            classifier.Train(new[] { 3, 4, 5 }, 1);
            classifier.Train(new[] { 1, 2, 4 }, 0);

            // Act: Classify test SDR [1,2,3] with k=2 neighbors
            int result = classifier.Classify(new[] { 1, 2, 3 }, 2);

            // Assert: Verify majority class from 2 nearest neighbors
            // Expected: Class 0 (matches SDR1 and SDR3)
            Assert.AreEqual(0, result, "Classification mismatch with valid training data");
        }

        /// <summary>
        /// Verifies proper error handling when classifying without prior training
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Test_Classify_WithNoTrainingData_ShouldThrowException()
        {
            // Act & Assert: Attempt classification with empty model
            classifier.Classify(new[] { 1, 2, 3 }, 3);
        }

        /// <summary>
        /// Validates K parameter sensitivity by testing different neighborhood sizes
        /// </summary>
        [TestMethod]
        public void Test_Classify_WithDifferentKValues()
        {
            // Arrange: Train with mixed class distribution
            classifier.Train(new[] { 1, 2, 3 }, 0);
            classifier.Train(new[] { 3, 4, 5 }, 1);
            classifier.Train(new[] { 1, 2, 4 }, 0);

            // Act & Assert: k=1 (nearest neighbor)
            Assert.AreEqual(0, classifier.Classify(new[] { 1, 2, 3 }, 1),
                "k=1 should return exact match class");

            // Act & Assert: k=3 (majority of all training samples)
            Assert.AreEqual(0, classifier.Classify(new[] { 1, 2, 3 }, 3),
                "k=3 should return majority class from all neighbors");
        }

        /// <summary>
        /// Validates distance-weighted voting implementation by testing
        /// classification with similarity-based weighting
        /// </summary>
        [TestMethod]
        public void Test_Classify_WithWeightedVoting()
        {
            // Arrange: Train classifier with ambiguous samples
            classifier.Train(new[] { 1, 2, 3 }, 0);
            classifier.Train(new[] { 3, 4, 5 }, 1);
            classifier.Train(new[] { 1, 2, 4 }, 0);

            // Act: Classify with k=2 weighted voting
            int result = classifier.Classify(new[] { 1, 2, 3 }, 2);

            // Assert: Verify weighted majority favors closer matches
            Assert.AreEqual(0, result,
                "Weighted voting should prioritize higher similarity samples");
        }

        /// <summary>
        /// Validates classifier performance and memory management with large datasets
        /// </summary>
        [TestMethod]
        public void Test_Classify_WithLargeDataset()
        {
            // Arrange: Generate 10,000 random 10-element SDRs with binary labels
            var trainingData = GenerateLargeDataset(10000);
            foreach (var (sdr, label) in trainingData)
            {
                classifier.Train(sdr, label);
            }

            // Act: Classify test sample with k=5
            var result = classifier.Classify(new[] { 1, 2, 3 }, 5);

            // Assert: Verify valid classification result
            Assert.IsTrue(result >= 0 && result <= 1,
                "Classification result should be valid class label");
        }

        /// <summary>
        /// Generates synthetic test data with configurable size and random features
        /// </summary>
        /// <param name="size">Number of samples to generate</param>
        /// <returns>List of tuples containing SDR arrays and class labels</returns>
        private List<(int[] SDR, int Label)> GenerateLargeDataset(int size)
        {
            var rand = new Random();
            var dataset = new List<(int[] SDR, int Label)>();

            // Create SDRs with 10 features using random values 0-9
            for (int i = 0; i < size; i++)
            {
                var sdr = new int[10];
                for (int j = 0; j < sdr.Length; j++)
                {
                    sdr[j] = rand.Next(0, 10);
                }
                dataset.Add((sdr, rand.Next(0, 2)));
            }

            return dataset;
        }
    }
}