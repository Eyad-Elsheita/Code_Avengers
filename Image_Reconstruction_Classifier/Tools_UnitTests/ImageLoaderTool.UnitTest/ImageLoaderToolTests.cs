using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Image_Reconstruction_Classifier.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ImageLoaderTool.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Image_Reconstruction_Classifier.Tools.ImageLoaderTool"/> — the MCP-callable
    /// tool wrapper, as opposed to the older <c>ImageLoader</c> helper class already covered elsewhere.
    /// Azure Blob Storage is mocked; no real Azure account is required to run these tests.
    /// </summary>
    [TestClass]
    public class ImageLoaderToolTests
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

        // BlobClient.DownloadToAsync(string) and DownloadToAsync(string, CancellationToken) are
        // distinct virtual overloads, not one method with a default parameter — the production
        // code calls the single-argument form, so that's the one that must be mocked.
        private static void SetupDownload(Mock<BlobClient> mockBlobClient, string content)
        {
            mockBlobClient
                .Setup(b => b.DownloadToAsync(It.IsAny<string>()))
                .Callback<string>(path => File.WriteAllText(path, content))
                .ReturnsAsync(Mock.Of<Response>());
        }

        [TestMethod]
        public async Task GetImageCount_CountsOnlyPngBlobs()
        {
            var mockContainer = CreateContainerClientMock("3_001.png", "3_001_binarized.txt", "3_002.png");
            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageLoaderTool(mockBlobService.Object);

            int count = await tool.GetImageCount("train");

            Assert.AreEqual(2, count, "Only .png blobs should be counted, not the .txt blob.");
        }

        [TestMethod]
        public async Task ListAvailableImages_ReturnsOnlyPngNames_InOriginalOrder()
        {
            var mockContainer = CreateContainerClientMock("3_001.png", "notes.txt", "3_002.png");
            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageLoaderTool(mockBlobService.Object);

            List<string> images = await tool.ListAvailableImages("train");

            CollectionAssert.AreEqual(new[] { "3_001.png", "3_002.png" }, images);
        }

        [TestMethod]
        public async Task LoadSingleImageFromBlob_ThrowsFileNotFoundException_WhenBlobMissing()
        {
            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(c => c.GetBlobClient("missing.png")).Returns(mockBlobClient.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageLoaderTool(mockBlobService.Object);

            await Assert.ThrowsExceptionAsync<FileNotFoundException>(
                () => tool.LoadSingleImageFromBlob("train", "missing.png"));
        }

        [TestMethod]
        public async Task LoadSingleImageFromBlob_ParsesDownloadedContent_IntoBinaryPixelArray()
        {
            // ImageLoader.LoadImage reads each *character* of each line: '1' -> 1, anything else -> 0.
            const string downloadedContent = "101\n010";

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient
                .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
            SetupDownload(mockBlobClient, downloadedContent);

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(c => c.GetBlobClient("3_001.png")).Returns(mockBlobClient.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageLoaderTool(mockBlobService.Object);

            int[] pixels = await tool.LoadSingleImageFromBlob("train", "3_001.png");

            CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1, 0 }, pixels);
        }

        [TestMethod]
        public async Task LoadImagesByObjectType_RespectsNumberOfImagesCap()
        {
            // Unique blob names (distinct from the other tests) so parallel test execution doesn't
            // collide on the same OS temp file path — ImageLoaderTool always downloads to a fixed
            // per-blob-name temp path, shared across every instance.
            var mockContainer = CreateContainerClientMock("7_101.png", "7_102.png", "7_103.png");

            var mockBlobClient = new Mock<BlobClient>();
            SetupDownload(mockBlobClient, "1");
            mockContainer.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageLoaderTool(mockBlobService.Object);

            int[][] images = await tool.LoadImagesByObjectType("train", "7", numberOfImages: 2);

            Assert.AreEqual(2, images.Length, "Loading should stop once the requested count is reached.");
        }
    }
}
