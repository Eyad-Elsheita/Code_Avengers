using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ImageSpatialTool.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Image_Reconstruction_Classifier.Tools.ImageSpatialTool"/>. Each call
    /// creates a fresh, untrained <c>SpatialPooler</c>, so an all-ones 784-element input vector is used
    /// as the known-good input (matching the pattern already proven in the existing
    /// UnitTests/ImageSpatial.UnitTest project) to reliably produce non-empty active columns.
    /// </summary>
    [TestClass]
    public class ImageSpatialToolTests
    {
        private static readonly string ValidVectorCsv = string.Join(",", Enumerable.Repeat(1, 784));

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
                .Setup(b => b.DownloadToAsync(It.IsAny<string>()))
                .Callback<string>(path => File.WriteAllText(path, content))
                .ReturnsAsync(Mock.Of<Response>());
            return mockBlobClient;
        }

        private static Mock<BlobClient> CreateUploadingBlobClientMock()
        {
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
            return mockBlobClient;
        }

        [TestMethod]
        public async Task TrainSingleImage_ThrowsInvalidDataException_WhenVectorLengthIsNot784()
        {
            const string blobName = "6_401_binarized.txt";

            var mockContainer = CreateContainerClientMock(blobName);
            mockContainer.Setup(c => c.GetBlobClient(blobName))
                .Returns(CreateDownloadingBlobClientMock("1,2,3").Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageSpatialTool(mockBlobService.Object);

            await Assert.ThrowsExceptionAsync<InvalidDataException>(
                () => tool.TrainSingleImage("train", blobName));
        }

        [TestMethod]
        public async Task TrainSingleImage_UploadsSpatialOutput_ForAValid784LengthVector()
        {
            const string blobName = "6_402_binarized.txt";
            const string expectedOutputName = "6_402_spatial.txt";

            var mockContainer = CreateContainerClientMock(blobName);
            mockContainer.Setup(c => c.GetBlobClient(blobName))
                .Returns(CreateDownloadingBlobClientMock(ValidVectorCsv).Object);

            var mockOutputBlob = CreateUploadingBlobClientMock();
            mockContainer.Setup(c => c.GetBlobClient(expectedOutputName)).Returns(mockOutputBlob.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageSpatialTool(mockBlobService.Object);

            string result = await tool.TrainSingleImage("train", blobName);

            StringAssert.Contains(result, blobName);
            mockOutputBlob.Verify(
                b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task InferSingleImage_UploadsSpatialOutput_ForAValid784LengthVector()
        {
            const string blobName = "6_403_binarized.txt";
            const string expectedOutputName = "6_403_spatial.txt";

            var mockContainer = CreateContainerClientMock(blobName);
            mockContainer.Setup(c => c.GetBlobClient(blobName))
                .Returns(CreateDownloadingBlobClientMock(ValidVectorCsv).Object);

            var mockOutputBlob = CreateUploadingBlobClientMock();
            mockContainer.Setup(c => c.GetBlobClient(expectedOutputName)).Returns(mockOutputBlob.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageSpatialTool(mockBlobService.Object);

            string result = await tool.InferSingleImage("train", blobName);

            StringAssert.Contains(result, blobName);
            mockOutputBlob.Verify(
                b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task TrainSpatialPooler_SkipsNonTxtBlobs_AndProcessesOnlyTxtOnes()
        {
            const string txtBlobName = "6_404_binarized.txt";
            const string pngBlobName = "6_404_binarized.png";
            const string expectedOutputName = "6_404_spatial.txt";

            var mockContainer = CreateContainerClientMock(txtBlobName, pngBlobName);
            mockContainer.Setup(c => c.GetBlobClient(txtBlobName))
                .Returns(CreateDownloadingBlobClientMock(ValidVectorCsv).Object);

            var mockOutputBlob = CreateUploadingBlobClientMock();
            mockContainer.Setup(c => c.GetBlobClient(expectedOutputName)).Returns(mockOutputBlob.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageSpatialTool(mockBlobService.Object);

            string result = await tool.TrainSpatialPooler("train");

            StringAssert.Contains(result, "Processed: 1");
            StringAssert.Contains(result, "Skipped: 1");
            mockContainer.Verify(c => c.GetBlobClient(pngBlobName), Times.Never);
        }
    }
}
