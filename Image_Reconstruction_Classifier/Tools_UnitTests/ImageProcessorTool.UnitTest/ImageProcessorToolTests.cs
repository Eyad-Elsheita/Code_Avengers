using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessorTool.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Image_Reconstruction_Classifier.Tools.ImageProcessorTool"/> — the MCP-callable
    /// binarization tool. Azure Blob Storage is mocked; the actual downloaded content is a real,
    /// tiny in-memory PNG so the underlying ImageBinarizer call is genuinely exercised, not stubbed out.
    /// </summary>
    [TestClass]
    public class ImageProcessorToolTests
    {
        private static byte[] CreateTinyGrayscalePng()
        {
            using var image = new Image<L8>(4, 4);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }

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

        [TestMethod]
        public async Task ConvertImagesToBinary_SkipsNonPngBlobs_AndProcessesOnlyPngOnes()
        {
            var mockContainer = CreateContainerClientMock("cpt_001.png", "cpt_001_binarized.txt", "cpt_notes.md");

            var mockInputBlob = new Mock<BlobClient>();
            mockInputBlob
                .Setup(b => b.DownloadToAsync(It.IsAny<string>()))
                .Callback<string>(path => File.WriteAllBytes(path, CreateTinyGrayscalePng()))
                .ReturnsAsync(Mock.Of<Response>());
            mockContainer.Setup(c => c.GetBlobClient("cpt_001.png")).Returns(mockInputBlob.Object);

            var mockOutputBlob = new Mock<BlobClient>();
            mockOutputBlob
                .Setup(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
            mockContainer.Setup(c => c.GetBlobClient("cpt_001_binarized.txt")).Returns(mockOutputBlob.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageProcessorTool(mockBlobService.Object);

            string result = await tool.ConvertImagesToBinary("train");

            StringAssert.Contains(result, "Processed: 1");
            StringAssert.Contains(result, "Skipped: 2");
            mockOutputBlob.Verify(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessBatch_CountsDownloadFailureAsSkipped_RatherThanThrowing()
        {
            var mockContainer = CreateContainerClientMock("5_201.png");

            var mockInputBlob = new Mock<BlobClient>();
            mockInputBlob
                .Setup(b => b.DownloadToAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("simulated transient Blob Storage failure"));
            mockContainer.Setup(c => c.GetBlobClient("5_201.png")).Returns(mockInputBlob.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageProcessorTool(mockBlobService.Object);

            string result = await tool.ProcessBatch("train", "5");

            StringAssert.Contains(result, "Processed: 0");
            StringAssert.Contains(result, "Skipped: 1");
        }

        [TestMethod]
        public async Task BinarizeImage_UploadsBinarizedOutput_ForAValidPng()
        {
            var mockInputBlob = new Mock<BlobClient>();
            mockInputBlob
                .Setup(b => b.DownloadToAsync(It.IsAny<string>()))
                .Callback<string>(path => File.WriteAllBytes(path, CreateTinyGrayscalePng()))
                .ReturnsAsync(Mock.Of<Response>());

            var mockOutputBlob = new Mock<BlobClient>();
            mockOutputBlob
                .Setup(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(c => c.GetBlobClient("9_050.png")).Returns(mockInputBlob.Object);
            mockContainer.Setup(c => c.GetBlobClient("9_050_binarized.txt")).Returns(mockOutputBlob.Object);

            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient("train")).Returns(mockContainer.Object);

            var tool = new Image_Reconstruction_Classifier.Tools.ImageProcessorTool(mockBlobService.Object);

            string result = await tool.BinarizeImage("train", "9_050.png");

            StringAssert.Contains(result, "9_050.png");
            mockOutputBlob.Verify(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
