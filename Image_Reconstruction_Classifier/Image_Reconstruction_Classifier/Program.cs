using Azure.Storage.Blobs;
using Image_Reconstruction_Classifier.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

try
{
    var builder = Host.CreateEmptyApplicationBuilder(settings: null);

    // Azure Blob Storage connection
    string blobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
        ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is not set.");

    builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
    builder.Services.AddHttpClient();

    // Register all tools
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<ImageLoaderTool>()
        .WithTools<ImageProcessorTool>()
        .WithTools<ImageSpatialTool>()
        .WithTools<HtmClassifierTool>()
        .WithTools<BinaryToImageConverterTool>()
        .WithTools<ImageSimilarityTool>()
        .WithTools<ImageFilterTool>();

    // Singleton registrations
    builder.Services.AddSingleton<ImageLoaderTool>();
    builder.Services.AddSingleton<ImageProcessorTool>();
    builder.Services.AddSingleton<ImageSpatialTool>();
    builder.Services.AddSingleton<HtmClassifierTool>();
    builder.Services.AddSingleton<BinaryToImageConverterTool>();
    builder.Services.AddSingleton<ImageSimilarityTool>();
    builder.Services.AddSingleton<ImageFilterTool>();

    Console.Error.WriteLine("Started Image Reconstruction MCP server");
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine("Application startup failed.");
    Console.Error.WriteLine(ex.ToString());
    throw;
}
