using Agents.Orchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Models;
using Plugins;
using Plugins.Native;
using System.Text.Json;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    configuration["OpenAI:ModelId"]!,
//    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Warning); });
var kernel = builder.Build();

kernel.Plugins.AddFromType<TransientPlugin>();

var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");

ChatCompletionAgent fireSafetyAgent = new()
{
    Name = "FireSafetyAgent",
    Description = "Fire Safety Agent that assesses fire danger",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the Fire Safety Agent that analyzis the environmental data and provide fire safety clearance.
        
        ## ACTIONS
        1. Activate emergency protocols if dangerous conditions detected (call fire detector)
        2. Grant or deny fire safety clearance based on environmental conditions

        ## CONTEXT
        Environmental report: tool ```load_environmental_report```

        ## OUTPUT TEMPLATE
        Respond with the fire safety clearance.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.2F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
fireSafetyAgent.Kernel.Plugins.AddFromType<FireDetectorPlugin>();

ChatCompletionAgent rainSafetyAgent = new()
{
    Name = "RainSafetyAgent",
    Description = "Rain Safety Agent that assesses rain danger",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the Rain Safety Agent that analyzis the environmental data and provide rain safety clearance.

        ## ACTIONS
        1. Activate emergency protocols if dangerous conditions detected (call rain detector)
        2. Grant or deny rain safety clearance based on environmental conditions

        ## CONTEXT
        Environmental report: tool ```load_environmental_report```
        
        ## OUTPUT TEMPLATE
        Respond with the rain safety clearance.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.2F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
rainSafetyAgent.Kernel.Plugins.AddFromType<RainDetectorPlugin>();

var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1F,
    MaxTokens = 1000,
    ResponseFormat = typeof(SafetyClearance)
};
StructuredOutputTransform<SafetyClearance> outputTransform = new(chat, executionSettings);

OrchestrationMonitor monitor = new(logger);

ConcurrentOrchestration<string, SafetyClearance> orchestration = new(fireSafetyAgent, rainSafetyAgent)
{
    ResultTransform = outputTransform.TransformAsync,
    ResponseCallback = monitor.ResponseCallback
};

var query = """
    MISSION COMMAND: Exploration Trip
    Evaluate the safety clearance
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# INPUT: {query}\n");
OrchestrationResult<SafetyClearance> result = await orchestration.InvokeAsync(query, runtime);
SafetyClearance response = await result.GetValueAsync(TimeSpan.FromMinutes(1));
Console.WriteLine($"\n# RESPONSES: {JsonSerializer.Serialize(response, new JsonSerializerOptions() { WriteIndented = true })}");

await runtime.RunUntilIdleAsync();
