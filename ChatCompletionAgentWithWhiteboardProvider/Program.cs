using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;
using OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    configuration["AzureOpenAI:DeploymentName"]!,
//    configuration["AzureOpenAI:Endpoint"]!,
//    configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
var kernel = builder.Build();

var embeddingGenerator = new OpenAIClient(configuration["OpenAI:ApiKey"]!)
    .GetEmbeddingClient(configuration["OpenAI:EmbeddingModelId"])
    .AsIEmbeddingGenerator();

var chat = kernel.GetRequiredService<IChatCompletionService>();
var chatClient = chat.AsChatClient();

var whiteboardProvider = new WhiteboardProvider(chatClient);

////var chatHistoryReducer = new ChatHistorySummarizationReducer(chat, targetCount: 3, thresholdCount: 2);
var chatHistoryReducer = new ChatHistoryTruncationReducer(targetCount: 3, thresholdCount: 2);

ChatHistoryAgentThread agentThread = new();
#pragma warning disable SKEXP0110 // AIContextProviders is experimental
agentThread.AIContextProviders.Add(whiteboardProvider);

ChatCompletionAgent agent = new()
{
    Name = "RobotCarAgent",
    Instructions = """
        You are an AI assistant.
        """,
    Description = "A robot car that can perform basic moves",
    LoggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>(),
    Kernel = kernel
};

Queue<string> ScriptLines = new();
ScriptLines.Enqueue("give me the definition of water in less than 10 words.");
ScriptLines.Enqueue("give me the definition of fire in less than 10 words.");
ScriptLines.Enqueue("give me the definition of earth in less than 10 words.");
ScriptLines.Enqueue("give me the definition of air in less than 10 words.");
ScriptLines.Enqueue("give me the definition of sun in less than 10 words.");
ScriptLines.Enqueue("give me the definition of moon in less than 10 words.");
ScriptLines.Enqueue("give me the definition of human in less than 10 words.");
ScriptLines.Enqueue("give me the definition of energy in less than 10 words.");
ScriptLines.Enqueue("give me the definition of void in less than 10 words.");

Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("RESPONSE:");
while (ScriptLines.Count > 0)
{
    var line = ScriptLines.Dequeue();
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }
    await foreach (var response in agent.InvokeAsync(line, agentThread))
    {
        Console.WriteLine($"*** {response.Message.Content}");
    }

    await agentThread.ChatHistory.ReduceInPlaceAsync(chatHistoryReducer, CancellationToken.None);
    await whiteboardProvider.WhenProcessingCompleteAsync();

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("Whiteboard contents:");
    foreach (var item in whiteboardProvider.CurrentWhiteboardContent)
    {
        Console.WriteLine($"  {item}");
    }
    Console.WriteLine();
    Console.ResetColor();
}
