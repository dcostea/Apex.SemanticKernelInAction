﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Orchestration;
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
        
        ## ACTIONS (STEPS)
        1. Read the sensor data and prepare the `environment report` using SensorsPlugin tools
            - temperature
            - humidity
            - wind speed
        2. Save the `environment report` using ```save_environment_report``` tool with argument `environment report`
        3. IF the temperature is higher than 50° Celsius, respond "HOT" followed by the `environment report`
        4. IF the humidity is higher than 70%, respond "WET" followed by the `environment report`
        5. IF the humidity is lower than 70% and temperature is lower than 50° Celsius, respond "SAFE" followed by the `environment report`
        
        ## CONSTRAINTS
        - Never terminate the mission command, always handoff to another agent.
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
        
        ## ACTIONS
        1. Read the environmental report using ```load_environment_report``` and activate the FireDetectorPlugin tool with data from it
        2. Activate the FireDetectorPlugin tool with data from the `environmental report`
        3. Immediately prepare and and save the `fire safety report` using tool ```save_fire_safety_report``` with argument `fire safety report`
        
        ## CONSTRAINTS
        - Never terminate the mission command, always handoff the `fire safety report` to MotorsAgent.
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
        
        ## ACTIONS
        1. Read the environmental report using ```load_environment_report``` and activate the RainDetectorPlugin tool with data from it
        2. Activate the RainDetectorPlugin tool with data from the `environmental report`
        3. Immediately prepare and and save the `rain safety report` using tool ```save_rain_safety_report``` with argument `rain safety report`

        ## CONSTRAINTS
        - Never terminate the mission command, always handoff the `rain safety report` to MotorsAgent.
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
        
        # ACTIONS (STEPS)
        1. ALWAYS call ```load_fire_safety_report``` and ```load_rain_safety_report```
            - If the fire safety report advises to stop, you stop.
            - If the rain safety report advises to proceed cautiously, you proceed cautiously.
            - If both reports are safe, you proceed with the original mission command.
        2. Otherwise (if no report found), proceed with the original mission command.
        3. Break down the mission command into a sequence of basic moves.
        4. Immediately execute the basic moves using the MotorsPlugin tools (functions).
        
        ## OUTPUT TEMPLATE
        Respond with the movements.
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

var monitor = new OrchestrationMonitor(logger);

HandoffOrchestration orchestration = new(
     OrchestrationHandoffs.StartWith(environmentAgent)
        .Add(environmentAgent, fireSafetyAgent, "Transfer to this agent if it's HOT")
        .Add(environmentAgent, rainSafetyAgent, "Transfer to this agent if it's WET")
        .Add(environmentAgent, motorsAgent, "Transfer to this agent if it's SAFE")
        .Add(fireSafetyAgent, motorsAgent)
        .Add(rainSafetyAgent, motorsAgent),
    environmentAgent,
    fireSafetyAgent,
    rainSafetyAgent,
    motorsAgent)
{
    ResponseCallback = monitor.ResponseCallback,
    InteractiveCallback = monitor.InteractiveCallback,
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
