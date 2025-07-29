using Agents.Orchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Models;
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

ChatCompletionAgent safetyAgent = new()
{
    Name = "SafetyAgent",
    Description = "Safety Agent that assesses fire and rain danger",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the Safety Agent that analysis the environmental report and provide safety clearance.

        ## ACTIONS
        1. Analyze the environmental report for any danger (fire or rain)
        2. Activate safety measures if fire or rain danger detected (using FireDetectorPlugin and RainDetectorPlugin tools)
        3. ALWAYS Provide safety clearance based on the analysis of environmental report

        ## CONSTRAINTS
        ALWAYS expect environmental report before proceeding with safety clearance.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.2F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
safetyAgent.Kernel.Plugins.AddFromType<FireDetectorPlugin>();
safetyAgent.Kernel.Plugins.AddFromType<RainDetectorPlugin>();

ChatCompletionAgent motorsAgent = new()
{
    Name = "MotorsAgent",
    Description = "Motors controller that breaks down a complex command into basic moves and executes them",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the Motors Agent that controls the robot car.

        ## ACTIONS
        1. If safety clearance is denied (not granted), STOP immediately
        2. If safety clearance is granted, proceed with the route from the original MISSION COMMAND
        3. Break down the route into basic movements such as {{$basic_moves}}
        4. Execute basic movements using the corresponding motor functions (using MotorsPlugin tools)

        ## OUTPUT TEMPLATE
        Respond only with the movements, without any additional explanations.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.3F,
        MaxTokens = 2000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
    { ["basic_moves"] = "forward, backward, turn left, turn right, and stop" }
};
// We do not need to add MotorsPlugin here, as we expect a structured output from MotorsAgent

#pragma warning disable SKEXP0110

var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1F,
    MaxTokens = 1000,
    ResponseFormat = typeof(StepsResult)
};
StructuredOutputTransform<StepsResult> outputTransform = new(chat, executionSettings);

OrchestrationMonitor monitor = new(logger);

SequentialOrchestration<string, StepsResult> orchestration = new(environmentAgent, safetyAgent, motorsAgent)
{
    ResultTransform = outputTransform.TransformAsync,
    ResponseCallback = monitor.ResponseCallback,
};

var query = """
    # MISSION COMMAND: Exploration Trip
    There is a tree directly in front of the car. Avoid it and then come back to the original path.
    """;

InProcessRuntime runtime = new();

await runtime.StartAsync();

Console.WriteLine($"# INPUT: {query}\n");
OrchestrationResult<StepsResult> result = await orchestration.InvokeAsync(query, runtime);
StepsResult response = await result.GetValueAsync(TimeSpan.FromMinutes(1));
Console.WriteLine($"\n# RESPONSE: {JsonSerializer.Serialize(response, new JsonSerializerOptions() { WriteIndented = true })}");

await runtime.RunUntilIdleAsync();

