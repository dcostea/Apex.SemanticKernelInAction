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
KernelFunction decrementingFunction = transientPlugin["decrement_number"];
KernelFunction multiplyingFunction = transientPlugin["multiply_number"];
KernelFunction loadCurrentNumberFunction = transientPlugin["load_current_number"];
KernelFunction saveCurrentNumberFunction = transientPlugin["save_current_number"];

var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");

ChatCompletionAgent starterAgent = new()
{
    Description = "Starter agent",
    Name = "StarterAgent",
    Kernel = kernel.Clone(),
    LoggerFactory = loggerFactory,
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the StarterAgent, an agent that starts the flow.
        Your responsibility is to dispatch numbers.
    
        ## CRITICAL ACTIONS
        1. FIRST, save the current number using tool ```save_current_number``` with argument `current number`
        2. Dispatch the number to the corresponding agent depending on wheather it is even or odd.
    """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 2000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
starterAgent.Kernel.Plugins.AddFromFunctions("TempStarterPlugin", [saveCurrentNumberFunction]);

ChatCompletionAgent firstWorkerAgent = new()
{
    Name = "EvenNumbersIncrementorAgent",
    Description = "Even numbers incrementor",
    LoggerFactory = loggerFactory,
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the EvenNumbersIncrementorAgent.

        ## CRITICAL ACTIONS
        1. FIRST, read the `current number` using tool ```load_current_number```
        2. Increment the `current number` by 3 using tool ```increment_number``` with argument `current number` and `amount` set to 3
        3. ALWAYS save the `current number` using tool ```save_current_number``` with argument `current number`
        4. IF the `current number` is a multiple of 5, dispatch it to MultipleOfFiveDoublerAgent
           ELSE respond with the `current number`
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
firstWorkerAgent.Kernel.Plugins.AddFromFunctions("TempEvenNumbersIncrementorPlugin", [loadCurrentNumberFunction, saveCurrentNumberFunction, incrementingFunction]);

ChatCompletionAgent secondWorkerAgent = new()
{
    Name = "OddNumbersDecrementorAgent",
    Description = "Odd numbers decrementor",
    LoggerFactory = loggerFactory,
    Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the OddNumbersDecrementorAgent.

        ## CRITICAL ACTIONS
        1. FIRST, read the `current number` using tool ```load_current_number```
        2. Decrement the `current number` by 2 using tool ```decrement_number``` with argument `current number` and `amount` set to 2
        3. ALWAYS save the `current number` using tool ```save_current_number``` with argument `current number`
        4. Respond with the `current number`.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
secondWorkerAgent.Kernel.Plugins.AddFromFunctions("TempOddNumbersIncrementorPlugin", [loadCurrentNumberFunction, saveCurrentNumberFunction, decrementingFunction]);

ChatCompletionAgent thirdWorkerAgent = new()
{
    Name = "MultipleOfFiveDoublerAgent",
    Description = "Multiplied multiples of five",
    LoggerFactory = loggerFactory,
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the MultipleOfFiveDoublerAgent.

        ## CRITICAL ACTIONS
        1. FIRST, read the `current number` using tool ```load_current_number```
        2. Multiply the `current number` by 10 using tool ```multiply_number``` with argument `current number` and `amount` set to 10
        3. Respond with the `current number`.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
thirdWorkerAgent.Kernel.Plugins.AddFromFunctions("TempMultipleOfFiveDoublerPlugin", [loadCurrentNumberFunction, saveCurrentNumberFunction, multiplyingFunction]);

OrchestrationHandoffs handoffs = new OrchestrationHandoffs(starterAgent)
    .Add(starterAgent, firstWorkerAgent, secondWorkerAgent)
    .Add(firstWorkerAgent, thirdWorkerAgent)
    .Add(secondWorkerAgent, thirdWorkerAgent)
    ;

var query = """
    number 12
    """;

await using InProcessRuntime runtime = new();
await runtime.StartAsync();

OrchestrationMonitor monitor = new(logger);

HandoffOrchestration orchestration = new(handoffs, starterAgent, firstWorkerAgent, secondWorkerAgent, thirdWorkerAgent)
{
    ResponseCallback = monitor.ResponseCallback,
    //StreamingResponseCallback = monitor.StreamingResponseCallback,
    LoggerFactory = loggerFactory,
};

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n# USER INPUT: {query}\n");
Console.ResetColor();

OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync();

await runtime.RunUntilIdleAsync();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n# RESPONSE: {response}");
Console.ResetColor();

