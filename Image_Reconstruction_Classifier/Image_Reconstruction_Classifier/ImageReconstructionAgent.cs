using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

namespace Image_Reconstruction_Classifier
{
    public class ImageReconstructionAgent
    {
        public static async Task RunAsync()
        {
            // Get OpenAI credentials
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY is not set.");

            string model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL_NAME")
                ?? "gpt-4o-mini";

            // Defaults to the local MCP server; set MCP_SERVER_URL to point at the Azure-hosted one instead.
            string mcpServerUrl = Environment.GetEnvironmentVariable("MCP_SERVER_URL")
                ?? "http://localhost:5280";

            CloudLogger.Log("ImageReconstructionAgent", $"Connecting to MCP server at {mcpServerUrl}");

            await using var mcpClient = await McpClient.CreateAsync(
                new HttpClientTransport(new()
                {
                    Name = "ImageReconstructionMCP",
                    Endpoint = new Uri(mcpServerUrl)
                }));

            CloudLogger.Log("ImageReconstructionAgent", "Connected to MCP server");

            // Load available tools
            var tools = await mcpClient.ListToolsAsync();

            CloudLogger.Log("ImageReconstructionAgent", $"Loaded {tools.Count()} tools:");
            foreach (var tool in tools)
                CloudLogger.LogInfo("ImageReconstructionAgent", $"  - {tool.Name}");

            // Create AI agent with all MCP tools
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
            await RunConversationLoopAsync(agent);
        }

        private static async Task RunConversationLoopAsync(AIAgent agent)
        {
            AgentSession session = await agent.CreateSessionAsync();

            Console.WriteLine("=== Image Reconstruction Agent ===");
            Console.WriteLine("Type 'exit' to quit.");
            Console.WriteLine("==================================");

            while (true)
            {
                Console.Write("\nYou: ");
                string userInput = Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(userInput) ||
                    userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Session ended.");
                    break;
                }

                try
                {
                    Console.Write("Agent: ");
                    var response = await agent.RunAsync(userInput, session);
                    Console.WriteLine(response);
                }
                catch (Exception ex)
                {
                    CloudLogger.LogError("ImageReconstructionAgent", "Error during agent run", ex);
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
