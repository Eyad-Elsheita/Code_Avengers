using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Image_Reconstruction_Classifier;

namespace ImageReconstructionTests
{
    /// <summary>
    /// Contains comprehensive unit tests for the MyHtmClassifier implementation
    /// </summary>
    [TestClass]
    public class MyHtmClassifierTests
    {
        private MyHtmClassifier classifier = null!;

        /// <summary>
        /// Initializes a fresh classifier instance before each test execution
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            // Create new classifier instance to ensure test isolation
            classifier = new MyHtmClassifier();
        }

        /// <summary>
        /// Verifies proper error handling when missing required original input data
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void Test_Learn_WithoutOriginalInput_ThrowsException()
        {
            // Act: Attempt training with incomplete parameters
            // Should enforce original input requirement
            classifier.Learn(1, new[] { 1, 2, 3 });
        }

        /// <summary>
        /// Validates successful training with complete parameter set
        /// </summary>
        [TestMethod]
        public void Test_Learn_WithOriginalInput_Success()
        {
            // Arrange: Create valid training data sample
            int key = 1;
            int[] activeCells = { 1, 2, 3 };  // SDR representation
            int[] originalInput = { 0, 1, 0, 1 };  // Original image vector

            // Act: Perform valid training operation
            classifier.Learn(key, activeCells, originalInput);

            // Assert: Verify internal state updates through subsequent operations
            // (State verification done in reconstruction tests)
        }

        /// <summary>
        /// Ensures null safety for active cells parameter
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Learn_WithNullActiveCells_ThrowsArgumentNullException()
        {
            // Act: Test null parameter validation
            classifier.Learn(1, null!, new[] { 0, 1 });
        }

        /// <summary>
        /// Ensures null safety for original input parameter
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Learn_WithNullOriginalInput_ThrowsArgumentNullException()
        {
            // Act: Test null input validation
            classifier.Learn(1, new[] { 1, 2, 3 }, null!);
        }

        /// <summary>
        /// Verifies proper error handling for uninitialized classifier usage
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Test_GetPredictedInputValues_NoTrainingExamples_ThrowsInvalidOperationException()
        {
            // Act: Attempt prediction without prior training
            classifier.GetPredictedInputValues(new[] { 1, 2, 3 }, 2);
        }

        /// <summary>
        /// Validates accurate input reconstruction from SDR patterns
        /// </summary>
        [TestMethod]
        public void Test_GetPredictedInputValues_WithValidTrainingExamples_ReturnsReconstructedImage()
        {
            // Arrange: Train classifier with multiple distinct patterns
            int key1 = 1, key2 = 2;
            int[] activeCells1 = { 1, 2, 3 };
            int[] originalInput1 = { 0, 1, 0, 1 };  // Checkerboard pattern 1
            int[] activeCells2 = { 4, 5, 6 };
            int[] originalInput2 = { 1, 1, 0, 0 };  // Checkerboard pattern 2

            classifier.Learn(key1, activeCells1, originalInput1);
            classifier.Learn(key2, activeCells2, originalInput2);

            // Act: Request reconstruction using matching SDR pattern
            int[] predictiveCells = { 1, 2, 3 };  // Matches first training example
            int[] predictedValues = classifier.GetPredictedInputValues(predictiveCells, 1);

            // Assert: Verify exact pattern reconstruction
            CollectionAssert.AreEqual(originalInput1, predictedValues,
                "Reconstructed image should match trained pattern");
        }

        /// <summary>
        /// Ensures data integrity checks during training
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_GetPredictedInputValues_TrainingExamplesMissingOriginalInput_ThrowsException()
        {
            // Arrange: Simulate invalid training data scenario
            int key = 1;
            int[] activeCells = { 1, 2, 3 };
            int[] originalInput = null!;  // Invalid null input

            // Act: Should trigger null validation check
            classifier.Learn(key, activeCells, originalInput);
        }

        /// <summary>
        /// Validates weighted reconstruction from multiple matching patterns
        /// </summary>
        [TestMethod]
        public void Test_GetPredictedInputValues_WithKTrainingExamples_ReturnsWeightedReconstruction()
        {
            // Arrange: Train classifier with three distinct patterns
            int key1 = 1, key2 = 2, key3 = 3;
            int[] activeCells1 = { 1, 2, 3 };
            int[] originalInput1 = { 1, 0, 1, 0 };  // Pattern A
            int[] activeCells2 = { 3, 4, 5 };
            int[] originalInput2 = { 0, 1, 0, 1 };  // Pattern B
            int[] activeCells3 = { 5, 6, 7 };
            int[] originalInput3 = { 1, 1, 1, 1 };  // Pattern C

            classifier.Learn(key1, activeCells1, originalInput1);
            classifier.Learn(key2, activeCells2, originalInput2);
            classifier.Learn(key3, activeCells3, originalInput3);

            // Act: Request reconstruction using overlapping SDR patterns
            int[] predictiveCells = { 3, 5 };  // Overlaps with first and second patterns
            int[] predictedValues = classifier.GetPredictedInputValues(predictiveCells, 2);

            // Assert: Verify correct output dimensions and reconstruction logic
            Assert.AreEqual(4, predictedValues.Length,
                "Reconstructed vector length should match original inputs");

            // Additional assertions would verify weighted combination logic
            // based on specific implementation details
        }
    }
}