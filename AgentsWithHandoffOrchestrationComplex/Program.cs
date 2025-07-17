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

var transientPlugin = kernel.Plugins.AddFromType<TransientPlugin>();
KernelFunction loadEnvironmentalReportFunction = transientPlugin["load_environmental_report"];
KernelFunction saveEnvironmentalReportFunction = transientPlugin["save_environmental_report"];
KernelFunction loadSafetyReportFunction = transientPlugin["load_safety_report"];
KernelFunction saveSafetyReportFunction = transientPlugin["save_safety_report"];
KernelFunction loadEmergencyPlanFunction = transientPlugin["load_emergency_plan"];
KernelFunction saveEmergencyPlanFunction = transientPlugin["save_emergency_plan"];

var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");

ChatCompletionAgent commanderAgent = new()
{
    Name = "StarterAgent",
    Description = "Starter agent",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are StarterAgent, an agent that starts the flow.
        Your responsibility is to dispatch the mission.

        ## CRITICAL ACTIONS
        1. Dispatch the mission to the corresponding agent.

        ## CONSTRAINTS
        - DO NOT attempt to load or save any reports, as this is not your responsibility.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 2000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};

ChatCompletionAgent environmentAgent = new()
{
    Name = "EnvironmentAgent",
    Description = "Environmental specialist that monitors weather and atmospheric conditions",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the EnvironmentAgent, an environmental sensor specialist.

        ## CRITICAL ACTIONS
        1. Read all available sensors immediately using SensorsPlugin tools
        2. Assemble a comprehensive `environmental report` based on the sensor readings
        3. ALWAYS save the `environmental report` using tool ```save_environmental_report``` with argument `environmental report`
        4. Hand off to SafetyAgent

        ## CONSTRAINTS
        - DO NOT stop the task or the flow.
        - CANNOT initiate safety measures or movements directly
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([saveEnvironmentalReportFunction!])
    })
};
environmentAgent.Kernel.Plugins.AddFromType<SensorsPlugin>();

ChatCompletionAgent safetyAgent = new()
{
    Name = "SafetyAgent",
    Description = "Safety agent that ensures safe operations",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the SafetyAgent, a specialist responsible for monitoring the safety systems.

        ## CRITICAL ACTIONS
        1. FIRST, read the `environmental report` using tool ```load_environmental_report```
        2. Activate the FireDetectorPlugin and RainDetectorPlugin tools using the previous `environmental report` data
        3. Assemble a comprehensive `safety report` based on the fire and rain detectors results
        4. ALWAYS save the `safety report` using tool ```save_safety_report``` with argument `safety report`
        5. IF fire emergency or rain conditions detected THEN:
           - 5.1 Hand off to NavigatorAgent
           ELSE:
           - 5.2 Hand off to SummarizerAgent

        ## CONSTRAINTS
        - DO NOT stop the task or the flow.
        - CANNOT initiate movement commands or read sensors directly
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([loadEnvironmentalReportFunction!, saveSafetyReportFunction!])
    })
};
safetyAgent.Kernel.Plugins.AddFromType<FireDetectorPlugin>();
safetyAgent.Kernel.Plugins.AddFromType<RainDetectorPlugin>();

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

        ## CRITICAL ACTIONS
        1. FIRST, read the `safety report` using tool ```load_safety_report```
        2. Assemble a comprehensive `safety assessment` based on the previous safety report
        3. Respond immediately with the `safety assessment`
        
        ## CONSTRAINTS
        - CANNOT initiate movement commands, safety measures, or read sensors directly
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([loadSafetyReportFunction!])
    })
};

ChatCompletionAgent navigatorAgent = new()
{
    Name = "NavigatorAgent",
    Description = "Navigator that reviews navigation routes",
    Kernel = kernel.Clone(),
    LoggerFactory = loggerFactory,
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        ## PERSONA
        You are the NavigatorAgent, a movement planning specialist.
        Your goal is to create optimal movement plans that account for environmental conditions and mission objectives.

        ## CRITICAL ACTIONS
        1. FIRST, read the `environmental report` using tool ```load_environmental_report```
        2. SECOND, read the `safety report` using tool ```load_safety_report```
        3. IF fire emergency conditions, generate immediate evacuation `plan` using backward and turn commands and hand off to MotorsAgent
        4. IF rain conditions detected, generate `plan` with reduced speeds and increased turn margins and hand off to MotorsAgent
        5. Save the `plan` using tool ```save_emergency_plan``` with argument `emergency_plan` and hand off to MotorsAgent
        6. IF normal conditions, do nothing and hand off to SummarizerAgent

        ## CONSTRAINTS
        - DO NOT stop the task or the flow.
        - Cannot execute movement commands directly
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.2F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([saveEmergencyPlanFunction!])
    })
};

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
        You are the MotorsAgent controlling a robot car capable of performing basic moves: forward, backward, turn_left, turn_right, stop.

        ## CRITICAL ACTIONS
        1. FIRST, read the emergency `plan` using tool ```load_emergency_plan```
        2. Break down any potential route (approximate) in the `plan` into basic moves you know.
        3. Respond only with the permitted moves, without any additional explanations.
        4. Execute the sequence of basic moves immediately.

        ## CONSTRAINTS
        - If error: execute `stop` command immediately and report a mechanical failure
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([loadEmergencyPlanFunction!])
    })
};
motorsAgent.Kernel.Plugins.AddFromType<MotorsPlugin>();


var monitor = new OrchestrationMonitor(logger);
//var manager = new ApprovalGroupChatManager(monitor, logger);

HandoffOrchestration orchestration = new(
     OrchestrationHandoffs.StartWith(commanderAgent)
        .Add(commanderAgent, environmentAgent)
        .Add(environmentAgent, safetyAgent)
        .Add(safetyAgent, summarizerAgent, navigatorAgent)
        .Add(navigatorAgent, motorsAgent)

        // back to starter: Transfer to this agent if the issue is not status related
        ,
    commanderAgent,
    environmentAgent,
    safetyAgent,
    navigatorAgent,
    motorsAgent,
    summarizerAgent
)
{
    ResponseCallback = monitor.ResponseCallback,
    LoggerFactory = loggerFactory,
};

var query = """
    MISSION: Safety assessment.
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# USER INPUT: {query}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync(TimeSpan.FromMinutes(5));
Console.WriteLine($"\n# RESPONSE: {response}");

Console.ResetColor();

await runtime.RunUntilIdleAsync();

