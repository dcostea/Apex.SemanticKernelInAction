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

var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");

ChatCompletionAgent environmentAgent = new()
{
    Name = "EnvironmentAgent",
    Description = "Environment agent that assembles the environment report",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    //Kernel = kernel,
    Instructions = """
        ## PERSONA
        You are the EnvironmentAgent, a specialist responsible for assembling the environment report from sensor data.

        ## ACTIONS
        1. Read the sensor data and prepare the `environment report` using SensorsPlugin tools
            - temperature
            - humidity
            - wind speed
        2. ALWAYS save the `environment report` using ```save_environment_report``` tool with argument `environment report`
        3. IF the temperature is higher than 50° Celsius" call ```HandoffPlugin-transfer_to_FireSafetyAgent``` to transfer to FireSafetyAgent
        4. IF the humidity is higher than 70%" call ```HandoffPlugin-transfer_to_RainSafetyAgent``` to transfer to RainSafetyAgent
        5. IF the humidity is lower than 70% and temperature is lower than 50° Celsius" call ```HandoffPlugin-transfer_to_MotorsAgent``` to transfer to MotorsAgent

        ## OUTPUT TEMPLATE
        Respond with the `environment report`.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
environmentAgent.Kernel.Plugins.AddFromType<SensorsPlugin>();
environmentAgent.Kernel.Plugins.AddFromType<TransientPlugin>();

ChatCompletionAgent fireSafetyAgent = new()
{
    Name = "FireSafetyAgent",
    Description = "Fire safety agent that ensures safe operations",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    //Kernel = kernel,
    Instructions = """
        ## PERSONA
        You are the FireSafetyAgent, a specialist responsible for preparing the fire safety report.

        ## CRITICAL ACTIONS
        1. Read the environmental report using ```load_environment_report``` and activate the FireDetectorPlugin tool with data from it
        2. Conclude the `fire safety report` based on the fire detectors result.
        3. Save the `fire safety report` using tool ```save_fire_safety_report``` with argument `fire safety report`
        4. ALWAYS call ```HandoffPlugin-transfer_to_MotorsAgent``` to transfer to MotorsAgent.

        ## OUTPUT TEMPLATE
        Respond with the `fire safety report`.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
fireSafetyAgent.Kernel.Plugins.AddFromType<FireDetectorPlugin>();
fireSafetyAgent.Kernel.Plugins.AddFromType<TransientPlugin>();

ChatCompletionAgent rainSafetyAgent = new()
{
    Name = "RainSafetyAgent",
    Description = "Rain safety agent that ensures safe operations",
    Kernel = kernel.Clone(),
    //UseImmutableKernel = true,
    //Kernel = kernel,
    Instructions = """
        ## PERSONA
        You are the RainSafetyAgent, a specialist responsible for preparing the rain safety report.

        ## CRITICAL ACTIONS
        1. Read the environmental report using ```load_environment_report``` and activate the RainDetectorPlugin tool with data from it
        2. Conclude the `rain safety report` based on the rain detectors result.
        3. Save the `rain safety report` using tool ```save_rain_safety_report``` with argument `rain safety report`
        4. ALWAYS call ```HandoffPlugin-transfer_to_MotorsAgent``` to transfer to MotorsAgent.

        ## OUTPUT TEMPLATE
        Respond with the `rain safety report`.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
rainSafetyAgent.Kernel.Plugins.AddFromType<RainDetectorPlugin>();
rainSafetyAgent.Kernel.Plugins.AddFromType<TransientPlugin>();

var monitor = new OrchestrationMonitor(logger);
//var manager = new ApprovalGroupChatManager(monitor, logger);

ChatCompletionAgent motorsAgent = new()
{
    Name = "MotorsAgent",
    Description = "Motors Agent",
    Kernel = kernel.Clone(),
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        # PERSONA
        You are the MotorsAgent responsible for controlling the robot car motors.
        The permitted basic moves are: forward, backward, turn left, turn right, and stop.
        
        # ACTIONS
        1. ALWAYS call ```load_fire_safety_report``` and ```load_rain_safety_report```
        2. IF no safety reports are found, proceed with the mission command.
        3. OTHERWISE integrate the safety reports in the mission command (e.g., if safety reports advice to stop, or proceed cautiously, you proceed immediately)
        4. Break down mission command into a sequence of basic moves and respond with the reasoning how you integrated the safety report AND with the basic moves sequence.
        5. Execute basic moves using the corresponding functions (using MotorsPlugin tools)
        
        ## OUTPUT TEMPLATE
        Respond with the movements, and with integration reasoning.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 2000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
motorsAgent.Kernel.Plugins.AddFromType<MotorsPlugin>();
motorsAgent.Kernel.Plugins.AddFromType<TransientPlugin>();

HandoffOrchestration orchestration = new(
     OrchestrationHandoffs.StartWith(environmentAgent)
        .Add(environmentAgent, fireSafetyAgent)
        .Add(environmentAgent, rainSafetyAgent)
        .Add(environmentAgent, motorsAgent)
        .Add(fireSafetyAgent, motorsAgent)
        .Add(rainSafetyAgent, motorsAgent)
        ,
    environmentAgent,
    fireSafetyAgent,
    rainSafetyAgent,
    motorsAgent
    )
{
    ResponseCallback = monitor.ResponseCallback,
    LoggerFactory = loggerFactory,
};

var query = """
    MISSION COMMAND:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path. The distance to the tree is 50 meters."

    You need safety report to proceed with the mission command!
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# USER INPUT: {query}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync(TimeSpan.FromMinutes(5));
Console.WriteLine($"\n# RESPONSE: {response}");

Console.ResetColor();

await runtime.RunUntilIdleAsync();
