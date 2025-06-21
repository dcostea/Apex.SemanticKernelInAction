using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;
using OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
var kernel = builder.Build();

var embeddingGenerator = new OpenAIClient(configuration["OpenAI:ApiKey"]!)
    .GetEmbeddingClient(configuration["OpenAI:EmbeddingModelId"])
    .AsIEmbeddingGenerator();
var vectorStore = new InMemoryVectorStore(new() { EmbeddingGenerator = embeddingGenerator });
#pragma warning disable SKEXP0130 // TextSearchStore is experimental
using var textSearchStore = new TextSearchStore<string>(vectorStore, collectionName: "FinancialData", vectorDimensions: 1536);
await textSearchStore.UpsertTextAsync(
[
    "June 1, 2025, Morning: 14°C, partly cloudy, wind 8 km/h, dry. Afternoon: 20°C, mostly sunny, wind 12 km/h, no rain. Night: 13°C, clear, wind 6 km/h, calm.",
    "June 2, 2025, Morning: 15°C, sunny, wind 10 km/h, dry. Afternoon: 22°C, mostly sunny, wind 14 km/h, dry roads. Night: 14°C, few clouds, wind 8 km/h, no precipitation.",
    "June 3, 2025, Morning: 13°C, cloudy, wind 10 km/h, dry. Afternoon: 21°C, clearing skies, wind 13 km/h, dry. Night: 13°C, mostly clear, wind 7 km/h, calm.",
    "June 4, 2025, Morning: 12°C, overcast, wind 11 km/h, dry. Afternoon: 19°C, showers likely, wind 16 km/h, wet roads possible. Night: 12°C, cloudy, wind 9 km/h, light drizzle.",
    "June 5, 2025, Morning: 12°C, cloudy, wind 13 km/h, occasional light rain. Afternoon: 18°C, overcast, wind 18 km/h, scattered rain showers. Night: 11°C, mostly cloudy, wind 10 km/h, some drizzle.",
    "June 6, 2025, Morning: 13°C, partly sunny, wind 9 km/h, dry. Afternoon: 20°C, mostly sunny, wind 12 km/h, dry. Night: 13°C, clear, wind 7 km/h, calm.",
    "June 7, 2025, Morning: 14°C, sunny, wind 10 km/h, dry. Afternoon: 22°C, mostly clear, wind 13 km/h, dry. Night: 14°C, few clouds, wind 8 km/h, calm.",
    "June 8, 2025, Morning: 15°C, sunny, wind 11 km/h, dry. Afternoon: 23°C, clear, wind 14 km/h, excellent visibility. Night: 15°C, clear, wind 9 km/h, calm.",
    "June 9, 2025, Morning: 14°C, partly cloudy, wind 10 km/h, dry. Afternoon: 21°C, increasing clouds, wind 13 km/h, dry. Night: 14°C, cloudy, wind 10 km/h, chance of light rain late.",
    "June 10, 2025, Morning: 13°C, cloudy, wind 12 km/h, possible drizzle. Afternoon: 19°C, scattered showers, wind 17 km/h, wet roads. Night: 12°C, overcast, wind 9 km/h, light rain possible.",
]);

ChatHistoryAgentThread agentThread = new();
var textSearchProvider = new TextSearchProvider(textSearchStore);
#pragma warning disable SKEXP0110 // AIContextProviders is experimental
agentThread.AIContextProviders.Add(textSearchProvider);

ChatCompletionAgent agent = new()
{
    Name = "RobotCarAgent",
    Instructions = """
        You are an AI assistant responding from the text context.
        """,
    Description = "A robot car assistant.",
    LoggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>(),
    Kernel = kernel
};

var query = """
    What is the average temperature on 2nd of June?
    Respond with the temperature in Celsius.
    """;

Console.WriteLine("RESPONSE:");
await foreach (var response in agent.InvokeAsync(query, agentThread))
{
    Console.WriteLine($"*** {response.Message.Content}");
}
