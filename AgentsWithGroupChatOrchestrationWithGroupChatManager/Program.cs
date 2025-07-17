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
        You are the NavigatorAgent responsible for evaluating and approving basic moves sequences for the robot car.

        # ACTIONS
        1. Analyze the basic moves sequence provided by MotorsAgent
        2. If the basic moves sequence is shortest and safe then respond with "APPROVED"
        3. If the basic moves sequence is not optimal or is unsafe suggest immediate adjustments
        
        # CONSTRAINTS
        ALWAYS keep at least 5 m distance from obstacles, the basic moves for robot car must be safe
        The basic moves sequence must be the shortest, you may use sharp diagonal turns (e.g., 30°, 45°, 60°)

        # OUTPUT
        Respond always 
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
                
        # ACTIONS
        1. Your have to break down complex commands into a sequence of these basic moves: forward, backward, turn left, turn right, and stop.
        2. If you have adjustments from NavigatorAgent, update the basic moves sequence (route) accordingly
        3. ALWAYS respond with the sequence of basic moves

        # CONSTRAINTS
        You can execute ONLY basic moves sequence approved by NavigatorAgent
        You cannot approve your own basic moves sequence

        # OUTPUT TEMPLATE
        Include duration or distance parameter (e.g., "forward 2m", "turn left 45°") with each basic move, except for `stop` move.
        """,
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.1F,
        MaxTokens = 1000,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};

var monitor = new OrchestrationMonitor(motorsAgent.Kernel);
var manager = new ApprovalGroupChatManager(monitor) 
{
    MaximumInvocationCount = 10,
    //InteractiveCallback = () =>
    //{
    //    ChatMessageContent input = new(AuthorRole.User, "In addition to what NavigatorAgent decides, increase the distance from objects from 5 to 10 meters for basic moves!");
    //    Console.WriteLine($"\n# USER INPUT: {input.Content}\n");
    //    return ValueTask.FromResult(input);
    //}
};

GroupChatOrchestration orchestration = new(manager, motorsAgent, navigatorAgent)
{
    ResponseCallback = monitor.ResponseCallback,
    LoggerFactory = loggerFactory,
};

var query = """
    # MISSION COMMAND:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path. The distance to the tree is 50 meters."
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# USER INPUT: {query}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync(TimeSpan.FromMinutes(5));
Console.WriteLine($"\n# RESPONSE: {response}");

Console.ResetColor();

await runtime.RunUntilIdleAsync();

