using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HtmClassifierTool.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Image_Reconstruction_Classifier.Tools.HtmClassifierTool"/>. Each trained
    /// example requires two paired blobs — a "*_spatial.txt" SDR file and a matching "*_binarized.txt"
    /// original-pixels file — both are mocked here; no real Azure account or NeoCortexApi data is needed.
    /// </summary>
    [TestClass]
    public class HtmClassifierToolTests
    {
        private static Mock<BlobContainerClient> CreateContainerClientMock(params string[] blobNames)
        {
            var pageOfItems = blobNames.Select(name => BlobsModelFactory.BlobItem(name: name)).ToList();
            var page = Page<BlobItem>.FromValues(pageOfItems, continuationToken: null, response: Mock.Of<Response>());
            AsyncPageable<BlobItem> pageable = AsyncPageable<BlobItem>.FromPages(new[] { page });

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer
                .Setup(c => c.GetBlobsAsync(
                    It.IsAny<BlobTraits>(),
                    It.IsAny<BlobStates>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(pageable);

            return mockContainer;
        }

        private static Mock<BlobClient> CreateDownloadingBlobClientMock(string content)
        {
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
            mockBlobClient
                .Setup(b => b.DownloadToAsync(It.IsAny<string>()))
                .Callback<string>(path => File.WriteAllText(path, content))
                .ReturnsAsync(Mock.Of<Response>());
            return mockBlobClient;
        }

        /// <summary>
        /// Wires up one complete, paired training example ("{prefix}_spatial.txt" + matching
        /// "{prefix}_binarized.txt") on the given container mock and returns the tool, already trained.
        /// </summary>
        private static async Task<(Image_Reconstruction_Classifier.Tools.HtmClassifierTool Tool, Mock<BlobContainerClient> Container)>
            TrainOneExampleAsync(string objectType, string index, string spatialSdrCsv, string binarizedPixelChars)
        {
            string spatialBlobName = $"{objectType}_{index}_spatial.txt";
            string binarizedBlobName = $"{objectType}_{index}_binarized.txt";

            var mockContainer = CreateContainerClientMock(spatialBlobName);
            mockContainer.Setup(c => c.GetBlobClient(spatialBlobName))
                .Returns(CreateDownloadingBlobClientMock(spatialSdrCsv).Object);
            mockContainer.Setup(c => c.GetBlobClient(binarizedBlobName))
                .Returns(CreateDownloadingBlobClientMock(binarizedPixelChars).Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.HtmClassifierTool(mockBlobService.Object);
            string trainResult = await tool.TrainClassifier("train", objectType);

            StringAssert.Contains(trainResult, "Trained: 1");
            StringAssert.Contains(trainResult, "Skipped: 0");

            return (tool, mockContainer);
        }

        [TestMethod]
        public async Task GetTrainedObjectTypes_ReturnsEmpty_BeforeAnyTraining()
        {
            var mockBlobService = new Mock<BlobServiceClient>();
            var tool = new Image_Reconstruction_Classifier.Tools.HtmClassifierTool(mockBlobService.Object);

            List<string> trained = await tool.GetTrainedObjectTypes();

            Assert.AreEqual(0, trained.Count);
        }

        [TestMethod]
        public async Task TrainClassifier_LearnsFromMatchingSpatialAndBinarizedBlobs()
        {
            var (tool, _) = await TrainOneExampleAsync(
                objectType: "6",
                index: "301",
                spatialSdrCsv: "1,2,3,4,5",
                binarizedPixelChars: "1010"); // ImageLoader.LoadImage: '1' -> 1, else -> 0

            List<string> trained = await tool.GetTrainedObjectTypes();

            CollectionAssert.Contains(trained, "6");
        }

        [TestMethod]
        public async Task ReconstructImage_ThrowsInvalidOperationException_ForUntrainedObjectType()
        {
            var mockBlobService = new Mock<BlobServiceClient>();
            var tool = new Image_Reconstruction_Classifier.Tools.HtmClassifierTool(mockBlobService.Object);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => tool.ReconstructImage("train", "9", "9_999_spatial.txt", k: 3));
        }

        [TestMethod]
        public async Task ReconstructImage_ReturnsTrainedPixels_AndUploadsResult_WhenQueriedWithTheTrainedSdr()
        {
            var (tool, mockContainer) = await TrainOneExampleAsync(
                objectType: "6",
                index: "302",
                spatialSdrCsv: "10,11,12,13,14",
                binarizedPixelChars: "0110");

            var mockOutputBlob = new Mock<BlobClient>();
            mockOutputBlob
                .Setup(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
            mockContainer.Setup(c => c.GetBlobClient("6_302_htm_reconstructed.txt")).Returns(mockOutputBlob.Object);

            // Re-download the same spatial blob for the reconstruction query itself.
            mockContainer.Setup(c => c.GetBlobClient("6_302_spatial.txt"))
                .Returns(CreateDownloadingBlobClientMock("10,11,12,13,14").Object);

            int[] reconstructed = await tool.ReconstructImage("train", "6", "6_302_spatial.txt", k: 1);

            // With a single training example and k=1, the classifier reproduces that example's pixels.
            CollectionAssert.AreEqual(new[] { 0, 1, 1, 0 }, reconstructed);
            mockOutputBlob.Verify(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
