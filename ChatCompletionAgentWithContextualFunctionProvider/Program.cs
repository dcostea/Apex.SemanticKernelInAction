using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Functions;
using OpenAI;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    configuration["AzureOpenAI:DeploymentName"]!,
//    configuration["AzureOpenAI:Endpoint"]!,
//    configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var sensorsPlugin = kernel.ImportPluginFromType<SensorsPlugin>();
var maintenancePlugin = kernel.ImportPluginFromType<MaintenancePlugin>();
var fireDetectorPlugin = kernel.ImportPluginFromType<FireDetectorPlugin>();
var rainDetectorPlugin = kernel.ImportPluginFromType<RainDetectorPlugin>();
var motorsPlugin = kernel.ImportPluginFromType<MotorsPlugin>();

var embeddingGenerator = new OpenAIClient(configuration["OpenAI:ApiKey"]!)
    .GetEmbeddingClient(configuration["OpenAI:EmbeddingModelId"])
    .AsIEmbeddingGenerator();

#pragma warning disable SKEXP0001 // RetainArgumentTypes is experimental
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
    Arguments = new(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions { RetainArgumentTypes = true }) })
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    }
};

ChatHistoryAgentThread agentThread = new();

#pragma warning disable SKEXP0110 // AIContextProviders is experimental
#pragma warning disable SKEXP0130 // ContextualFunctionProvider is experimental
var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions { EmbeddingGenerator = embeddingGenerator });
agentThread.AIContextProviders.Add(
    new ContextualFunctionProvider(
        vectorStore,
        vectorDimensions: 1536,
        functions: [..fireDetectorPlugin, ..rainDetectorPlugin, ..sensorsPlugin, ..maintenancePlugin, ..motorsPlugin, ],
        maxNumberOfFunctions: 5,
        loggerFactory: kernel.Services.GetRequiredService<ILoggerFactory>()
    )
);

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE:");
await foreach (var response in agent.InvokeAsync(query, agentThread))
{
    Console.WriteLine(response.Message.Content);
}
