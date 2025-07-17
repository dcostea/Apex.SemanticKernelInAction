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
        You are the NavigatorAgent responsible for approving basic moves sequences for the robot car.

        # ACTIONS
        1. Ask for the basic moves sequence provided from MotorsAgent
        2. If the basic moves sequence is optimal and safe then respond with "APPROVED"
        3. If the basic moves sequence is not optimal or is unsafe respond immediately with suggested improvements

        # CONSTRAINTS
        ALWAYS keep at least 5 m distance from obstacles, the basic moves for robot car must be safe
        The basic moves sequence must be optimal, you may use sharp diagonal turns (e.g., 30°, 45°, 60°)
        
        # OUTPUT
        ALWAYS respond to MotorsAgent requests with either "APPROVED" or suggested improvements.
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
    Description = "Motors that execute basic moves sequences for the robot car.",
    Kernel = kernel.Clone(),
    LoggerFactory = loggerFactory,
    //Kernel = kernel,
    //UseImmutableKernel = true,
    Instructions = """
        # PERSONA
        You are the MotorsAgent responsible for executing basic moves sequences for the robot car.

        # ACTIONS
        1. Break down (if they are not already basic!) the complex command into a sequence of these basic moves: forward, backward, turn left, turn right, and stop.
        2. ONLY IF APPROVED by NavigatorAgent, execute the sequence of basic moves

        # OUTPUT TEMPLATE
        Include duration or distance parameter (e.g., "forward 2m", "turn left 45°") with each basic move, except for `stop` move.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.2F,
        MaxTokens = 1000,
    })
};

var monitor = new OrchestrationMonitor();
var manager = new ApprovalGroupChatManager(monitor) 
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

