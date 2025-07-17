using Agents.Orchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins;

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

var transientPlugin = kernel.Clone().Plugins.AddFromType<TransientPlugin>();
KernelFunction incrementingFunction = transientPlugin["increment_number"];
KernelFunction loadCurrentNumberFunction = transientPlugin["load_current_number"];
KernelFunction saveCurrentNumberFunction = transientPlugin["save_current_number"];
KernelFunction loadStopConditionFunction = transientPlugin["load_stop_condition"];
KernelFunction saveStopConditionFunction = transientPlugin["save_stop_condition"];

var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");

ChatCompletionAgent starterAgent = new()
{
    Description = "Starter agent",
    Name = "StarterAgent",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    LoggerFactory = loggerFactory,
    Instructions = """
        ## PERSONA
        You are the StarterAgent, an agent that starts the flow.
        Your responsibility is to dispatch numbers.
    
        ## CRITICAL ACTIONS
        1. Save the `current number` using tool ```save_current_number``` with argument `current number`
        2. Save the `stop condition` using tool ```save_stop_condition``` with argument `stop_condition`
        3. Hand off to IncrementorAgent

        ## CONSTRAINTS
        You are NOT ALLOWED to do any calculations or logic.
    """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 2000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
starterAgent.Kernel.Plugins.AddFromFunctions("TempStarterPlugin", [saveCurrentNumberFunction, saveStopConditionFunction]);

ChatCompletionAgent incrementorAgent = new()
{
    Name = "IncrementorAgent",
    Description = "Numbers incrementor agent",
    LoggerFactory = loggerFactory,
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = false,
    Instructions = """
        ## PERSONA
        You are the IncrementorAgent.
        Your responsibility is to increment the current number.

        ## CRITICAL ACTIONS
        1. FIRST load the `current number` using tool ```load_current_number```
        2. THEN Increment the `current number` using tool ```increment_number``` with argument `current number` and 'amount' = 1
        3. THEN save the `current number` using tool ```save_current_number``` with argument `current number`
        4. ALWAYS hand off to CheckerAgent

        ## CONSTRAINTS
        - DO NOT report/respond with any progress, just hand off to CheckerAgent
        - NEVER hand off to CheckerAgent if you have the FINAL RESULT, instead respond with the FINAL RESULT `current number` immediately
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
incrementorAgent.Kernel.Plugins.AddFromFunctions("TempIncrementorPlugin", [loadCurrentNumberFunction, saveCurrentNumberFunction, incrementingFunction]);

ChatCompletionAgent checkerAgent = new()
{
    Name = "CheckerAgent",
    Description = "Numbers stop condition checker agent",
    LoggerFactory = loggerFactory,
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the CheckerAgent.
        Your responsibility is to check the current number against the stop condition and respond with the final result, when you have it.
        
        ## CRITICAL ACTIONS
        1. Load the `current number` using tool ```load_current_number```
        2. Load the `stop condition` using tool ```load_stop_condition```
        3. IF the `current number` is greater or equal to `stop condition` respond with FINAL RESULT: `current number` and STOP the task immediatelly.
        4. ELSE (if the `current number` is lower than `stop condition`) hand off to IncrementorAgent

        ## CONSTRAINTS
        - DO NOT report any progress, just respond with the FINAL RESULT, when you have it
        - NEVER hand off to IncrementorAgent if you have the FINAL RESULT
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
checkerAgent.Kernel.Plugins.AddFromFunctions("TempCheckerPlugin", [loadCurrentNumberFunction, loadStopConditionFunction]);

OrchestrationHandoffs handoffs = new OrchestrationHandoffs(starterAgent)
    .Add(starterAgent, incrementorAgent)
    .Add(incrementorAgent, checkerAgent)
    .Add(checkerAgent, incrementorAgent, "Transfer to this agent until you meet the stop condition")
    ;

var query = """
    Process the number 12.
    Stop condition is 16.
    """;

// Create a cancellation token source with a timeout
using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(65));
var cancellationToken = cancellationTokenSource.Token;

OrchestrationMonitor monitor = new(logger);

HandoffOrchestration orchestration = new(handoffs, starterAgent, incrementorAgent, checkerAgent)
{
    ResponseCallback = monitor.ResponseCallback,
    //StreamingResponseCallback = monitor.StreamingResponseCallback,
    LoggerFactory = loggerFactory,
};

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n# USER INPUT: {query}\n");
Console.ResetColor();

await using InProcessRuntime runtime = new();
await runtime.StartAsync();

OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime, cancellationToken);
string response = await result.GetValueAsync();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n# RESPONSE: {response}");
Console.ResetColor();

await runtime.RunUntilIdleAsync();

