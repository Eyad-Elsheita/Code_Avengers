using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Image_Reconstruction_Classifier;

namespace ImageReconstructionTests
{
    [TestClass]
    public class MyHtmClassifierTests
    {
        private MyHtmClassifier classifier;

        [TestInitialize]
        public void SetUp()
        {
            // Set up a new instance of MyHtmClassifier for each test
            classifier = new MyHtmClassifier();
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void Test_Learn_WithoutOriginalInput_ThrowsException()
        {
            // Act
            classifier.Learn(1, new[] { 1, 2, 3 });
        }

        [TestMethod]
        public void Test_Learn_WithOriginalInput_Success()
        {
            // Arrange
            int key = 1;
            int[] activeCells = { 1, 2, 3 };
            int[] originalInput = { 0, 1, 0, 1 };

            // Act
            classifier.Learn(key, activeCells, originalInput);

            // Assert
            // No exception should occur, and the classifier should now hold one training example
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Learn_WithNullActiveCells_ThrowsArgumentNullException()
        {
            // Act
            classifier.Learn(1, null, new[] { 0, 1 });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Learn_WithNullOriginalInput_ThrowsArgumentNullException()
        {
            // Act
            classifier.Learn(1, new[] { 1, 2, 3 }, null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Test_GetPredictedInputValues_NoTrainingExamples_ThrowsInvalidOperationException()
        {
            // Act
            classifier.GetPredictedInputValues(new[] { 1, 2, 3 }, 2);
        }

        [TestMethod]
        public void Test_GetPredictedInputValues_WithValidTrainingExamples_ReturnsReconstructedImage()
        {
            // Arrange
            int key1 = 1, key2 = 2;
            int[] activeCells1 = { 1, 2, 3 };
            int[] originalInput1 = { 0, 1, 0, 1 };
            int[] activeCells2 = { 4, 5, 6 };
            int[] originalInput2 = { 1, 1, 0, 0 };

            classifier.Learn(key1, activeCells1, originalInput1);
            classifier.Learn(key2, activeCells2, originalInput2);

            int[] predictiveCells = { 1, 2, 3 }; // Overlaps with the first training example

            // Act
            int[] predictedValues = classifier.GetPredictedInputValues(predictiveCells, 1);

            // Assert
            CollectionAssert.AreEqual(originalInput1, predictedValues, "Predicted input values do not match expected reconstruction.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_GetPredictedInputValues_TrainingExamplesMissingOriginalInput_ThrowsException()
        {
            // Arrange
            int key = 1;
            int[] activeCells = { 1, 2, 3 };
            int[] originalInput = null; // Passing null for originalInput

            // Act
            classifier.Learn(key, activeCells, originalInput);

            // Assert
            // No need for additional assertions since the ExpectedException attribute handles this.
        }


        [TestMethod]
        public void Test_GetPredictedInputValues_WithKTrainingExamples_ReturnsWeightedReconstruction()
        {
            // Arrange
            int key1 = 1, key2 = 2, key3 = 3;
            int[] activeCells1 = { 1, 2, 3 };
            int[] originalInput1 = { 1, 0, 1, 0 };
            int[] activeCells2 = { 3, 4, 5 };
            int[] originalInput2 = { 0, 1, 0, 1 };
            int[] activeCells3 = { 5, 6, 7 };
            int[] originalInput3 = { 1, 1, 1, 1 };

            classifier.Learn(key1, activeCells1, originalInput1);
            classifier.Learn(key2, activeCells2, originalInput2);
            classifier.Learn(key3, activeCells3, originalInput3);

            int[] predictiveCells = { 3, 5 }; // Overlaps with two training examples

            // Act
            int[] predictedValues = classifier.GetPredictedInputValues(predictiveCells, 2);

            // Assert
            Assert.AreEqual(4, predictedValues.Length, "Reconstructed image should match the length of original inputs.");
        }
    }
}
