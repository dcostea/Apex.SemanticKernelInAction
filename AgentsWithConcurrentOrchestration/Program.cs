using Agents.Orchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins.Native;

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

var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");

ChatCompletionAgent environmentAgent = new()
{
    Name = "EnvironmentAgent",
    Description = "Environmental specialist that monitors weather and atmospheric conditions",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the Environment Agent that reads sensors and provide environmental report.
        Before any exploration steps, you must read sensors for temperature, humidity, rain drops, and wind speed.

        ## ACTIONS
        1. Read sensors for temperature, humidity, rain drops, wind speed (use SensorsPlugin tools)
        2. Provide clear environmental report for other agents to use
        
        ## OUTPUT TEMPLATE
        Respond only with the environmental report, without any additional explanations.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.5F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
environmentAgent.Kernel.Plugins.AddFromType<SensorsPlugin>();

ChatCompletionAgent fireSafetyAgent = new()
{
    Name = "FireSafetyAgent",
    Description = "Fire Safety Agent that assesses fire danger",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the Fire Safety Agent that monitors fire conditions and provide fire safety clearance.

        ## ACTIONS
        1. Activate fire emergency protocols for dangerous conditions detection (call fire detector)
        2. Grant or deny fire safety clearance based on environmental conditions

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
        You are the Rain Safety Agent that monitors rain conditions and provide rain safety clearance.

        ## ACTIONS
        1. Activate rain emergency protocols for dangerous conditions detection (call rain detector)
        2. Grant or deny rain safety clearance based on environmental conditions

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

OrchestrationMonitor monitor = new(logger);

ConcurrentOrchestration orchestration = new(environmentAgent, fireSafetyAgent, rainSafetyAgent)
{
    ResponseCallback = monitor.ResponseCallback,
};

var query = """
    MISSION COMMAND: Exploration Trip
    Assess the environmental conditions and ensure safety clearance.
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# INPUT: {query}\n");
OrchestrationResult<string[]> result = await orchestration.InvokeAsync(query, runtime);
string[] responses = await result.GetValueAsync(TimeSpan.FromMinutes(1));
Console.WriteLine($"\n# RESPONSES: \n---\n{string.Join("\n---\n", responses)}");

await runtime.RunUntilIdleAsync();

