using AgentsWithGroupChatOrchestration;
using AgentsWithConcurrentOrchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    configuration["OpenAI:ModelId"]!,
//    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Trace);
    ////logging.AddConsole();
    logging.AddSeq("http://localhost:5341");
});
var kernel = builder.Build();

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
        # PERSONA
        You are the NavigatorAgent responsible for approving or denying the proposed sequences of basic moves for the robot car.

        # ACTIONS
        1. If the `proposed sequence` IS NOT optimal (see optimality rule below), respond with "DENIED" followed by optimality tips.
        2. Otherwise, if the `proposed sequence` IS optimal, respond with "APPROVED" immediately.

        # RULES
        - Optimality rule: Utilize turning angles of 30°, 45°, or 60° for efficient pathing.

        # OUTPUT TEMPLATE
        ALWAYS respond with "APPROVED" or "DENIED: followed by optimality tips"
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
        # PERSONA
        You are the MotorsAgent responsible for controlling the robot car motors.
        The permitted basic moves are: forward, backward, turn left, turn right, and stop.

        # ACTIONS
        1. Break down mission command into a sequence of basic moves (considering the optimality suggestions from NavigatorAgent, if any).
        2. If "APPROVED" by NavigatorAgent: respond with the final sequence and execute it using MotorsPlugin.
        3. OTHERWISE, if "DENIED" by NavigatorAgent: revise the sequence based on feedback and resubmit for approval.
        
        # CONSTRAINTS
        - ALWAYS wait for NavigatorAgent approval before executing any sequence.
        - NEVER respond with "APPROVED".
        - NEVER execute a "DENIED" sequence.
                
        # OUTPUT TEMPLATE
        Respond with the `proposed sequence` or with the approved `final sequence`.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};

var monitor = new OrchestrationMonitor(motorsAgent.Kernel);
var manager = new MonitoredApprovalGroupChatManager(monitor) 
{
    MaximumInvocationCount = 10,
    InteractiveCallback = monitor.InteractiveCallback
};

GroupChatOrchestration orchestration = new(manager, navigatorAgent, motorsAgent)
{
    ResponseCallback = monitor.ResponseCallback,
    LoggerFactory = loggerFactory,
};

var query = """
    # Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path. The distance to the tree is 50 meters."
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# USER INPUT: {query}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync(TimeSpan.FromMinutes(2));
Console.WriteLine($"\n# RESPONSE: {response}");

Console.ResetColor();

await runtime.RunUntilIdleAsync();

