using System.ComponentModel;
using Azure.Storage.Blobs;
using ModelContextProtocol.Server;
using NeoCortexApi;
using NeoCortexApi.Entities;

namespace Image_Reconstruction_Classifier.Tools
{
    [McpServerToolType]
    public class ImageSpatialTool
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string InputContainer = "input";
        private const string OutputContainer = "output";
        private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "ImageSpatial");

        public ImageSpatialTool(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            Directory.CreateDirectory(_tempFolder);
        }

        // Shared HTM Spatial Pooler initialization to avoid duplication
        private static (SpatialPooler, Connections) InitializeSpatialPooler()
        {
            SpatialPooler spatialPooler = new SpatialPooler();
            Connections connections = new Connections();

            connections.HtmConfig.InputDimensions = new int[] { 784 };
            connections.HtmConfig.ColumnDimensions = new int[] { 2048 };
            connections.HtmConfig.PotentialRadius = 12;
            connections.HtmConfig.PotentialPct = 0.8;
            connections.HtmConfig.GlobalInhibition = true;
            connections.HtmConfig.LocalAreaDensity = 0.03;
            connections.HtmConfig.StimulusThreshold = 5;
            connections.HtmConfig.SynPermInactiveDec = 0.008;
            connections.HtmConfig.SynPermActiveInc = 0.05;
            connections.HtmConfig.SynPermConnected = 0.2;

            spatialPooler.Init(connections);
            return (spatialPooler, connections);
        }
    }
}