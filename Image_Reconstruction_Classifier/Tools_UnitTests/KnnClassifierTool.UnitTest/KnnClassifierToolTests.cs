using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace KnnClassifierTool.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Image_Reconstruction_Classifier.Tools.KnnClassifierTool"/>.
    ///
    /// Note: the tool keeps its active classifier in a <c>static</c> field shared across every
    /// instance in the process, so state from one test can, in principle, leak into another.
    /// This class intentionally runs sequentially (no [assembly: Parallelize] here) and every
    /// prediction test trains with a large, test-unique SDR value range plus an exact-match
    /// query at k=1, so its own trained example always has the maximum possible overlap and
    /// wins the vote regardless of what other tests have added to the shared classifier.
    /// </summary>
    [TestClass]
    public class KnnClassifierToolTests
    {
        [TestMethod]
        public async Task TrainKnnClassifier_ThrowsArgumentNullException_WhenSdrIsNull()
        {
            var mockBlobService = new Mock<BlobServiceClient>();
            var tool = new Image_Reconstruction_Classifier.Tools.KnnClassifierTool(mockBlobService.Object);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => tool.TrainKnnClassifier(null!, label: 1));
        }

        [TestMethod]
        public async Task TrainKnnClassifier_And_PredictWithKnn_ReturnsTrainedLabel_ForExactMatch()
        {
            var mockBlobService = new Mock<BlobServiceClient>();
            var tool = new Image_Reconstruction_Classifier.Tools.KnnClassifierTool(mockBlobService.Object);

            int[] sdr = { 91001, 91002, 91003, 91004, 91005 };
            await tool.TrainKnnClassifier(sdr, label: 42);

            int predicted = await tool.PredictWithKnn((int[])sdr.Clone(), k: 1);

            Assert.AreEqual(42, predicted);
        }

        [TestMethod]
        public async Task SaveModelToBlob_UploadsSerializedTrainingData()
        {
            var mockBlobService = new Mock<BlobServiceClient>();
            var tool = new Image_Reconstruction_Classifier.Tools.KnnClassifierTool(mockBlobService.Object);

            // Ensure the shared classifier has at least one example to serialize.
            await tool.TrainKnnClassifier(new[] { 92001, 92002 }, label: 7);

            var mockOutputBlob = new Mock<BlobClient>();
            mockOutputBlob
                .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(c => c.GetBlobClient("my-model.knn.json")).Returns(mockOutputBlob.Object);

            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            string result = await tool.SaveModelToBlob("my-model", "train");

            StringAssert.Contains(result, "my-model.knn.json");
            mockOutputBlob.Verify(
                b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task LoadModelFromBlob_ThrowsFileNotFoundException_WhenBlobMissing()
        {
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(c => c.GetBlobClient("missing-model.knn.json")).Returns(mockBlobClient.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.KnnClassifierTool(mockBlobService.Object);

            await Assert.ThrowsExceptionAsync<FileNotFoundException>(
                () => tool.LoadModelFromBlob("missing-model", "train"));
        }

        [TestMethod]
        public async Task LoadModelFromBlob_ReplacesActiveClassifier_WithDownloadedTrainingData()
        {
            const string json = """[{"SDR":[93001,93002,93003],"Label":99}]""";

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
            mockBlobClient
                .Setup(b => b.DownloadContentAsync())
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobDownloadResult(content: BinaryData.FromString(json)),
                    Mock.Of<Response>()));

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(c => c.GetBlobClient("loaded-model.knn.json")).Returns(mockBlobClient.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.KnnClassifierTool(mockBlobService.Object);

            string result = await tool.LoadModelFromBlob("loaded-model", "train");

            StringAssert.Contains(result, "loaded-model.knn.json");

            int predicted = await tool.PredictWithKnn(new[] { 93001, 93002, 93003 }, k: 1);
            Assert.AreEqual(99, predicted, "Predicting on the exact SDR that was just loaded should return its label.");
        }
    }
}
