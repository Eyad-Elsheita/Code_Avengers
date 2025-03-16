using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageReconstructionTests
{

    [TestClass]
    public class KnnClassifierTests
    {
        private KnnClassifier classifier;

        [TestInitialize]
        public void SetUp()
        {
            // Testten önce çalışacak her şey burada yapılır
            classifier = new KnnClassifier();
        }

        [TestMethod]
        public void Test_Classify_WithCorrectTrainingData()
        {
            // Arrange
            int[] sdr1 = { 1, 2, 3 };
            int[] sdr2 = { 3, 4, 5 };
            int[] sdr3 = { 1, 2, 4 };

            classifier.Train(sdr1, 0);
            classifier.Train(sdr2, 1);
            classifier.Train(sdr3, 0);

            // Act
            int result = classifier.Classify(new int[] { 1, 2, 3 }, 2);

            // Assert
            Assert.AreEqual(0, result); // Class 0 should be the predicted class as it has higher overlap
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Test_Classify_WithNoTrainingData_ShouldThrowException()
        {
            // Act
            classifier.Classify(new int[] { 1, 2, 3 }, 3);
        }

        [TestMethod]
        public void Test_Classify_WithDifferentKValues()
        {
            // Arrange
            int[] sdr1 = { 1, 2, 3 };
            int[] sdr2 = { 3, 4, 5 };
            int[] sdr3 = { 1, 2, 4 };

            classifier.Train(sdr1, 0);
            classifier.Train(sdr2, 1);
            classifier.Train(sdr3, 0);

            // Act & Assert for k=1
            int resultK1 = classifier.Classify(new int[] { 1, 2, 3 }, 1);
            Assert.AreEqual(0, resultK1); // Only sdr1 should be considered for k=1

            // Act & Assert for k=3
            int resultK3 = classifier.Classify(new int[] { 1, 2, 3 }, 3);
            Assert.AreEqual(0, resultK3); // Class 0 should still be predicted for k=3
        }

        [TestMethod]
        public void Test_Classify_WithWeightedVoting()
        {
            // Arrange
            int[] sdr1 = { 1, 2, 3 };
            int[] sdr2 = { 3, 4, 5 };
            int[] sdr3 = { 1, 2, 4 };

            classifier.Train(sdr1, 0);
            classifier.Train(sdr2, 1);
            classifier.Train(sdr3, 0);

            // Act
            int result = classifier.Classify(new int[] { 1, 2, 3 }, 2);

            // Assert
            Assert.AreEqual(0, result); // Class 0 should have a higher score due to weighted overlap
        }

        [TestMethod]
        public void Test_Classify_WithLargeDataset()
        {
            // Arrange
            var trainingData = GenerateLargeDataset(10000);
            foreach (var (sdr, label) in trainingData)
            {
                classifier.Train(sdr, label);
            }

            // Act
            var result = classifier.Classify(new int[] { 1, 2, 3 }, 5);

            // Assert
            Assert.IsTrue(result >= 0); // Ensure the result is a valid label
        }

        // Helper method to generate a large dataset with random SDRs
        private List<(int[] SDR, int Label)> GenerateLargeDataset(int size)
        {
            var rand = new Random();
            var dataset = new List<(int[] SDR, int Label)>();

            for (int i = 0; i < size; i++)
            {
                var sdr = new int[10]; // Fixed SDR size of 10 for simplicity
                for (int j = 0; j < sdr.Length; j++)
                {
                    sdr[j] = rand.Next(0, 10); // Random values between 0 and 9
                }

                dataset.Add((sdr, rand.Next(0, 2))); // Random label (0 or 1)
            }

            return dataset;
        }
    }
}