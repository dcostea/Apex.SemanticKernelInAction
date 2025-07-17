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

// Just creating an isolated plugin with the neede functions
var transientPlugin = kernel.Clone().Plugins.AddFromType<TransientPlugin>();

KernelFunction loadSafetyClearanceFunction = transientPlugin["load_safety_clearance"];
KernelFunction saveSafetyClearanceFunction = transientPlugin["save_safety_clearance"];

var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");

ChatCompletionAgent commanderAgent = new()
{
    Name = "CommanderAgent",
    Description = "Commander agent",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are CommanderAgent, an agent that starts the mission.
        Your responsibility is to control the flow.

        ## CRITICAL ACTIONS
        1. ALWAYS FIRST load the `safety clearance` using tool ```load_safety_clearance```
        2. IF the mission is complete, DO NOT transfer to SafetyAgent, respond with mission status.
        3. IF the `safety clearance` is denied, DO NOT transfer to SafetyAgent, respond with mission status.
        4. IF there is no `safety clearance` THEN transfer to SafetyAgent to generate it.

        ## CONSTRAINTS
        - DO NOT attempt to load or save any reports, as this is not your responsibility.
        - NEVER hand off to SafetyAgent if the safety clearance is denied or mission is complete.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 2000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Required([loadSafetyClearanceFunction])
    })
};
commanderAgent.Kernel.ImportPluginFromFunctions("TempTransient", [loadSafetyClearanceFunction]);

ChatCompletionAgent safetyAgent = new()
{
    Name = "SafetyAgent",
    Description = "Safety agent that ensures safe operations",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the SafetyAgent, a specialist responsible for granting or denying the safety clearance.

        ## CRITICAL ACTIONS
        1. Read all available sensors immediately using SensorsPlugin tools
        2. Activate the FireDetectorPlugin and RainDetectorPlugin tools using the data from sensors
        3. Assemble a `safety clearance` based on the fire and rain detectors results (granted or denied).
        4. ALWAYS save the `safety clearance` using tool ```save_safety_clearance``` with argument `safety clearance`
        5. IF clearance is granted THEN hand off to MotorsAgent

        ## CONSTRAINTS
        - DO NOT stop the task or the flow.
        - CANNOT initiate movement commands
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Required([saveSafetyClearanceFunction])
    })
};
safetyAgent.Kernel.Plugins.AddFromType<SensorsPlugin>();
safetyAgent.Kernel.Plugins.AddFromType<FireDetectorPlugin>();
safetyAgent.Kernel.Plugins.AddFromType<RainDetectorPlugin>();
safetyAgent.Kernel.ImportPluginFromFunctions("TempTransient", [saveSafetyClearanceFunction]);

ChatCompletionAgent motorsAgent = new()
{
    Name = "MotorsAgent",
    Description = "Motors controller",
    Kernel = kernel.Clone(),
    LoggerFactory = loggerFactory,
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the MotorsAgent controlling a robot car capable of performing basic moves.

        ## CRITICAL ACTIONS
        1. Break down the MISSION COMMAND into basic moves like: forward, backward, turn_left, turn_right, stop.
        2. Respond only with the permitted moves, without any additional explanations.
        3. Execute the sequence of basic moves immediately.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Required([loadSafetyClearanceFunction])
    })
};
motorsAgent.Kernel.Plugins.AddFromType<MotorsPlugin>();
motorsAgent.Kernel.ImportPluginFromFunctions("TempTransient", [loadSafetyClearanceFunction]);

ChatCompletionAgent summarizerAgent = new()
{
    Name = "SummarizerAgent",
    Description = "Summarizes the flow",
    LoggerFactory = loggerFactory,
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the SummarizerAgent.
        Your responsibility is to summarize the flow of the mission.

        ## CRITICAL ACTIONS
        1. Respond immediately with the final outcome of the mission
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([loadSafetyClearanceFunction])
    })
};
summarizerAgent.Kernel.ImportPluginFromFunctions("TempTransient", [loadSafetyClearanceFunction]);

var monitor = new OrchestrationMonitor(logger);
//var manager = new ApprovalGroupChatManager(monitor, logger);

HandoffOrchestration orchestration = new(
     OrchestrationHandoffs.StartWith(commanderAgent)
        .Add(commanderAgent, safetyAgent, "Transfer to this agent if no safety clearance")
        .Add(safetyAgent, summarizerAgent, "Transfer to this agent if the safety clearance is denied")
        .Add(safetyAgent, motorsAgent)
        ,
    commanderAgent,
    safetyAgent,
    motorsAgent,
    summarizerAgent
)
{
    ResponseCallback = monitor.ResponseCallback,
    LoggerFactory = loggerFactory,
};

var query = """
    MISSION COMMAND:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path. The distance to the tree is 50 meters."

    You need safety clearance granted to proceed with the mission command!
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# USER INPUT: {query}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync(TimeSpan.FromMinutes(5));
Console.WriteLine($"\n# RESPONSE: {response}");

Console.ResetColor();

await runtime.RunUntilIdleAsync();

