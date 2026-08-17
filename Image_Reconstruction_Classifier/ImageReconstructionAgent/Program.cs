using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using ModelContextProtocol.Client;
using OpenAI;

try
{
    // Get OpenAI credentials
    string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");

    string model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL_NAME")
        ?? "gpt-4o-mini";

    // MCP server URL — your Azure App Service
    string mcpServerUrl = Environment.GetEnvironmentVariable("MCP_SERVER_URL")
        ?? "https://image-reconstruction-app.azurewebsites.net";

    Console.WriteLine($"Connecting to MCP server at {mcpServerUrl}");

    // Connect to MCP server via HTTP
    await using var mcpClient = await McpClient.CreateAsync(
        new HttpClientTransport(new()
        {
            Name = "ImageReconstructionMCP",
            Endpoint = new Uri(mcpServerUrl)
        }));

    Console.WriteLine("Connected to MCP server");

    // Load available tools
    var tools = await mcpClient.ListToolsAsync();

    Console.WriteLine($"\nLoaded {tools.Count()} tools:");
    foreach (var tool in tools)
        Console.WriteLine($"  - {tool.Name}");

    // Create AI agent
    List<AITool> aiTools = [.. tools.Cast<AITool>()];

    AIAgent agent =
        new OpenAIClient(apiKey)
            .GetChatClient(model)
            .AsIChatClient()
            .AsAIAgent(
                instructions: """
                    You are an image reconstruction assistant.
                    You help train HTM and KNN classifiers on images stored in Azure Blob Storage,
                    reconstruct images, evaluate quality using similarity metrics,
                    and save results to Azure Table Storage.
                    Always use the available tools to perform operations.
                    Containers available: 'train' for training images, 'test' for test images.
                    """,
                tools: aiTools);

    // Start conversation loop
    AgentSession session = await agent.CreateSessionAsync();

    Console.WriteLine("\n=== Image Reconstruction Agent ===");
    Console.WriteLine("Type 'exit' to quit.");
    Console.WriteLine("==================================\n");

    while (true)
    {
        Console.Write("You: ");
        string userInput = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userInput) ||
            userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Session ended.");
            break;
        }

        try
        {
            var response = await agent.RunAsync(userInput, session);
            Console.WriteLine($"Agent: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Startup failed: {ex.Message}");
    throw;
}