using Azure.Storage.Blobs;
using Image_Reconstruction_Classifier;
using Image_Reconstruction_Classifier.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

/// <summary>
/// Entry point. Builds the ASP.NET Core host, wires up Azure Blob/Table Storage clients, and
/// registers every Tools/*.cs class as both an MCP tool and a DI singleton, then serves them
/// over HTTP via ModelContextProtocol.AspNetCore so the agent can reach them locally or in Azure.
/// </summary>
try
{
    var builder = WebApplication.CreateBuilder(args);

    // Bind to all interfaces on a predictable port unless the host (e.g. Azure App Service) already sets one.
    // Binding to "localhost" would make the server unreachable from outside a container.
    if (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is null)
    {
        builder.WebHost.UseUrls("http://0.0.0.0:5280");
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
        .WithTools<KnnClassifierTool>()
        .WithTools<BinaryToImageConverterTool>()
        .WithTools<ImageSimilarityTool>()
        .WithTools<ImageFilterTool>();

    // Singleton registrations
    builder.Services.AddSingleton<ImageLoaderTool>();
    builder.Services.AddSingleton<ImageProcessorTool>();
    builder.Services.AddSingleton<ImageSpatialTool>();
    builder.Services.AddSingleton<HtmClassifierTool>();
    builder.Services.AddSingleton<KnnClassifierTool>();
    builder.Services.AddSingleton<BinaryToImageConverterTool>();
    builder.Services.AddSingleton<ImageSimilarityTool>();
    builder.Services.AddSingleton<ImageFilterTool>();

    var app = builder.Build();
    app.MapMcp();

    CloudLogger.Log("Program", "Starting Image Reconstruction MCP server");
    await app.RunAsync();
}
catch (Exception ex)
{
    CloudLogger.LogError("Program", "Application startup failed", ex);
    throw;
}
