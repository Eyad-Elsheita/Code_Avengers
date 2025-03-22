using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Image_Reconstruction_Classifier;

namespace ImageReconstructionTests
{
    [TestClass]
    public class MyHtmClassifierTests
    {
        // The 'null!' operator is used here to suppress the non-nullable warning,
        // as the field will be initialized in the TestInitialize method.
        private MyHtmClassifier classifier = null!;

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
            // Act: Passing only key and activeCells should throw NotImplementedException.
            classifier.Learn(1, new[] { 1, 2, 3 });
        }

        [TestMethod]
        public void Test_Learn_WithOriginalInput_Success()
        {
            // Arrange
            int key = 1;
            int[] activeCells = { 1, 2, 3 };
            int[] originalInput = { 0, 1, 0, 1 };

            // Act: Learn using both activeCells and originalInput should work fine.
            classifier.Learn(key, activeCells, originalInput);

            // Assert: No exception should occur, and the classifier should now hold one training example.
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Learn_WithNullActiveCells_ThrowsArgumentNullException()
        {
            // Act: Use null! to bypass nullable warnings. This is intentional to test exception handling.
            classifier.Learn(1, null!, new[] { 0, 1 });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Learn_WithNullOriginalInput_ThrowsArgumentNullException()
        {
            // Act: Use null! for originalInput to intentionally trigger ArgumentNullException.
            classifier.Learn(1, new[] { 1, 2, 3 }, null!);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Test_GetPredictedInputValues_NoTrainingExamples_ThrowsInvalidOperationException()
        {
            // Act: With no training examples, calling GetPredictedInputValues should throw InvalidOperationException.
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

            // Learn two different training examples
            classifier.Learn(key1, activeCells1, originalInput1);
            classifier.Learn(key2, activeCells2, originalInput2);

            // Arrange predictive cells that overlap with the first training example
            int[] predictiveCells = { 1, 2, 3 };

            // Act: Get the predicted input values using the predictiveCells.
            int[] predictedValues = classifier.GetPredictedInputValues(predictiveCells, 1);

            // Assert: The reconstructed image should match the first training example's original input.
            CollectionAssert.AreEqual(originalInput1, predictedValues, "Predicted input values do not match expected reconstruction.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_GetPredictedInputValues_TrainingExamplesMissingOriginalInput_ThrowsException()
        {
            // Arrange: Passing null! for originalInput intentionally to simulate a missing original input.
            int key = 1;
            int[] activeCells = { 1, 2, 3 };
            int[] originalInput = null!;

            // Act: This should throw ArgumentNullException.
            classifier.Learn(key, activeCells, originalInput);
        }

        [TestMethod]
        public void Test_GetPredictedInputValues_WithKTrainingExamples_ReturnsWeightedReconstruction()
        {
            // Arrange multiple training examples for a weighted reconstruction.
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

            // Arrange predictive cells that overlap with two training examples.
            int[] predictiveCells = { 3, 5 };

            // Act: Get the predicted values using k=2 training examples.
            int[] predictedValues = classifier.GetPredictedInputValues(predictiveCells, 2);

            // Assert: The length of the reconstructed image should match the original inputs.
            Assert.AreEqual(4, predictedValues.Length, "Reconstructed image should match the length of original inputs.");
        }
    }
}
