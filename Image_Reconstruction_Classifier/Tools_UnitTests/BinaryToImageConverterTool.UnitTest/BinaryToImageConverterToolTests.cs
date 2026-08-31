using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BinaryToImageConverterTool.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Image_Reconstruction_Classifier.Tools.BinaryToImageConverterTool"/> —
    /// the MCP-callable tool that renders a reconstructed binary array as a PNG and uploads it.
    /// Azure Blob Storage is mocked; no real Azure account is required to run these tests.
    /// </summary>
    [TestClass]
    public class BinaryToImageConverterToolTests
    {
        private static Mock<BlobServiceClient> CreateBlobServiceMock(
            string containerName, Mock<BlobContainerClient> mockContainer)
        {
            var mockBlobService = new Mock<BlobServiceClient>();
            mockBlobService.Setup(s => s.GetBlobContainerClient(containerName)).Returns(mockContainer.Object);
            return mockBlobService;
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
        public async Task ConvertBinaryToImage_ThrowsArgumentException_WhenArrayLengthDoesNotMatchDimensions()
        {
            var mockBlobService = new Mock<BlobServiceClient>();
            var tool = new Image_Reconstruction_Classifier.Tools.BinaryToImageConverterTool(mockBlobService.Object);

            int[] binaryImage = { 1, 0, 1 }; // length 3, but 2x2 = 4 expected

            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => tool.ConvertBinaryToImage("train", binaryImage, width: 2, height: 2, outputBlobName: "bad.png"));
        }

        [TestMethod]
        public async Task ConvertBinaryToImage_UploadsRenderedPng_AndReturnsSuccessMessage()
        {
            var mockOutputBlob = CreateUploadingBlobClientMock();
            var mockContainer = new Mock<BlobContainerClient>();
            mockContainer.Setup(c => c.GetBlobClient("recon_001.png")).Returns(mockOutputBlob.Object);

            var mockBlobService = CreateBlobServiceMock("train", mockContainer);
            var tool = new Image_Reconstruction_Classifier.Tools.BinaryToImageConverterTool(mockBlobService.Object);

            int[] binaryImage = { 1, 0, 0, 1 }; // 2x2

            string result = await tool.ConvertBinaryToImage("train", binaryImage, width: 2, height: 2, outputBlobName: "recon_001.png");

            StringAssert.Contains(result, "recon_001.png");
            StringAssert.Contains(result, "train");
            mockOutputBlob.Verify(b => b.UploadAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task SaveReconstructedImage_UploadsUnder_LabelIndexReconstructedNamingConvention()
        {
            var mockOutputBlob = CreateUploadingBlobClientMock();
            var mockContainer = new Mock<BlobContainerClient>();
            // The naming convention under test: "{label}_{index}_reconstructed.png"
            mockContainer.Setup(c => c.GetBlobClient("3_007_reconstructed.png")).Returns(mockOutputBlob.Object);

            var mockBlobService = CreateBlobServiceMock("test", mockContainer);
            var tool = new Image_Reconstruction_Classifier.Tools.BinaryToImageConverterTool(mockBlobService.Object);

            int[] binaryImage = { 1, 1, 0, 0 };

            await tool.SaveReconstructedImage("test", binaryImage, width: 2, height: 2, label: "3", index: "007");

            mockContainer.Verify(c => c.GetBlobClient("3_007_reconstructed.png"), Times.Once);
        }

        [TestMethod]
        public async Task BatchConvert_CountsEachImageIndependently_SuccessAndFailure()
        {
            var mockOutputBlob = CreateUploadingBlobClientMock();
            var mockContainer = new Mock<BlobContainerClient>();
            // Any output blob name in this batch resolves to the same uploading mock.
            mockContainer.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockOutputBlob.Object);

            var mockBlobService = CreateBlobServiceMock("train", mockContainer);
            var tool = new Image_Reconstruction_Classifier.Tools.BinaryToImageConverterTool(mockBlobService.Object);

            int[][] binaryImages =
            {
                new[] { 1, 0, 0, 1 }, // valid 2x2
                new[] { 1, 0, 1 },    // invalid — length 3, expected 4
                new[] { 0, 1, 1, 0 }, // valid 2x2
            };

            string result = await tool.BatchConvert("train", binaryImages, width: 2, height: 2, filePrefix: "batch");

            StringAssert.Contains(result, "Success: 2");
            StringAssert.Contains(result, "Failed: 1");
        }
    }
}
