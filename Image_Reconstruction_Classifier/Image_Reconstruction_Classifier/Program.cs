using Azure.Storage.Blobs;
using Image_Reconstruction_Classifier;
using Image_Reconstruction_Classifier.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

try
{
    if (args.Length > 0 && args[0] == "--agent")
    {
        await ImageReconstructionAgent.RunAsync();
        return;
    }

    var builder = WebApplication.CreateBuilder(args);

    // Give the server a predictable local address unless the host (e.g. Azure App Service) already sets one.
    if (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is null)
    {
        builder.WebHost.UseUrls("http://localhost:5280");
    }

    // Azure Blob Storage connection
    string blobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
        ?? throw new InvalidOperationException("AZURE_STORAGE_CONNECTION_STRING is not set.");

    builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
    builder.Services.AddSingleton(new ResultStorageService(blobConnectionString));
    builder.Services.AddHttpClient();

    // Register all tools, served over HTTP so the agent (local or hosted in Azure) can reach them.
    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
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

    var app = builder.Build();
    app.MapMcp();

    CloudLogger.Log("Program", $"Started Image Reconstruction MCP server at {string.Join(", ", app.Urls)}");
    await app.RunAsync();
}
catch (Exception ex)
{
    CloudLogger.LogError("Program", "Application startup failed", ex);
    throw;
}
