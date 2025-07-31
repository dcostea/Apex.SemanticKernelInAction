using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    configuration["AzureOpenAI:DeploymentName"]!,
//    configuration["AzureOpenAI:Endpoint"]!,
//    configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
var kernel = builder.Build();

ChatCompletionAgent agent = new()
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    LoggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>(),
    Kernel = kernel,
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
            You have to break down the provided complex commands into basic moves you know.
            Respond only with the permitted moves, without any additional explanations.
            """)),
    Arguments = new()
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    },
    HistoryReducer = new ChatHistorySummarizationReducer(
        kernel.GetRequiredService<IChatCompletionService>(),
        targetCount:  2,
        thresholdCount: 3 
    )
};

ChatHistoryAgentThread agentThread = new();

do
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var query = Console.ReadLine();
    if (string.IsNullOrEmpty(query)) break;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Assistant > ");

    var kernelArguments = new KernelArguments()
    {
        ["query"] = query
    };

    Console.ForegroundColor = ConsoleColor.Green;
    await foreach (var response in agent.InvokeStreamingAsync(query, agentThread))
    {
        Console.Write(response.Message.Content);
    }
    Console.WriteLine();

    Console.WriteLine($"@ Message Count: {agentThread?.ChatHistory.Count}");

    if (await agent.ReduceAsync(agentThread!.ChatHistory))
    {
        int summaryIndex = 0;
        while (agentThread!.ChatHistory[summaryIndex].Metadata?.ContainsKey(ChatHistorySummarizationReducer.SummaryMetadataKey) ?? false)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Summary: {agentThread.ChatHistory[summaryIndex++].Content}");
            Console.ResetColor();
        }
    }
}
while (true);
