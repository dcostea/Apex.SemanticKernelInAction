using Agents.Orchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!, 
    serviceId: "");
//builder.AddOpenAIChatCompletion(
//    configuration["OpenAI:ModelId"]!,
//    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Warning); });
var kernel = builder.Build();

var builder2 = Kernel.CreateBuilder();
builder2.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:Deployment3"]!,
    configuration["AzureOpenAI:Endpoint3"]!,
    configuration["AzureOpenAI:ApiKey3"]!,
    serviceId: "");
//builder.AddOpenAIChatCompletion(
//    configuration["OpenAI:ModelId"]!,
//    configuration["OpenAI:ApiKey"]!);
builder2.Services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Warning); });
var kernel2 = builder2.Build();



var loggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Microsoft.SemanticKernel");


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
        You are a NavigatorAgent for a rover robot.
        Your task is to review, refine, and mark `approved` a sequence of moves proposed by the MotorsAgent, if it is safe and optimal.
        
        ## CRITICAL ACTIONS
        1. Review the incoming plan. If it violates any safety and optimality rules, provide a safer and more optimal sequence of moves. 
        2. If the plan is already good, respond immediately with final: 'STATUS: APPROVED'.
        
        ## RULES
        1. **Safety:** The robot must never collide with an obstacle. It must leave at least 5 meters clearance from any identified obstacle.
        2. **Optimality:** The robot should use efficient turning angles. Replace any 90-degree turns with two 45-degree turns or a single 30-degree turn, where appropriate.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.2F,
        MaxTokens = 1000,
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
        You are the MotorsAgent responsible for controlling the robot car motors.
        Your have to break down complex commands into a sequence of these basic moves: forward, backward, turn left, turn right, and stop.
                
        ## CRITICAL ACTIONS
        1. Given the mission command, generate a preliminary list of steps to accomplish the goal.
        2. Do not worry about perfect safety or optimality; just create a functional first draft, NavigatorAgent will suggest improvements.
        3. If you received a message with 'STATUS: APPROVED' from NavigatorAgent, execute the attached tools with the provided plan and respond with 'EXECUTION COMPLETE'.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};
motorsAgent.Kernel.Plugins.AddFromType<MotorsPlugin>();

OrchestrationMonitor monitor = new(logger);

#pragma warning disable SKEXP0110

var chat = kernel2.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings();

var magenticManager = new StandardMagenticManager(chat, executionSettings)
{
    MaximumInvocationCount = 5,    // Total agent invocations allowed
    //MaximumResetCount = 2,          // How many times the manager can reset
    //MaximumStallCount = 3,          // Consecutive non-productive turns 
};

MagenticOrchestration orchestration = new(magenticManager, motorsAgent, navigatorAgent)
{
    ResponseCallback = monitor.ResponseCallback,
    //StreamingResponseCallback = monitor.StreamingResponseCallback,
    LoggerFactory = loggerFactory,
};

var query = """
    MISSION COMMAND:
    "There is a tree directly in front of the car. The distance to the tree is 50 meters."
    """;

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n# USER INPUT: {query}\n");
Console.ResetColor();

InProcessRuntime runtime = new();
await runtime.StartAsync();

OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync(TimeSpan.FromMinutes(5));

await runtime.RunUntilIdleAsync();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\n# RESPONSE: {response}");
Console.ResetColor();
