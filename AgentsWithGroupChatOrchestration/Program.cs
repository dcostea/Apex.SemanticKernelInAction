using AgentsWithGroupChatOrchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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
        3. If the basic moves sequence is not shortest or is unsafe suggest immediate adjustments
        
        # CONSTRAINTS
        ALWAYS keep at least 5 m distance from obstacles, the basic moves for robot car must be safe
        The basic moves sequence must be the shortest, you may use sharp diagonal turns (e.g., 30°, 45°, 60°)

        # OUTPUT
        Respond always with the approval state and with the suggested adjustments for safety or optimization, if any.
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
motorsAgent.Kernel.Plugins.AddFromType<MotorsPlugin>();

var monitor = new OrchestrationMonitor();
var manager = new ApprovalGroupChatManager()
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
    # MISSION COMMAND: Exploration Trip
    "There is a tree directly in front of the car. Avoid it and then come back to the original path. The distance to the tree is 50 meters."
    """;

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# INPUT: {query}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
string response = await result.GetValueAsync();
Console.WriteLine($"\n# RESPONSE: {response}");

Console.ResetColor();

await runtime.RunUntilIdleAsync();


/*

 * -------------------------------------------------
 GroupChatOrchestration with RoundRobin: Not Like a Series of Sequential Orchestrators
No, GroupChatOrchestration with RoundRobinGroupChatManager is fundamentally different from a series of Sequential Orchestrators. While they may appear similar on the surface, they operate on completely different architectural principles that create distinct behaviors and capabilities.

Key Architectural Differences
Context Sharing Model
GroupChatOrchestration: All agents participate in a shared conversation history. Every agent can see what all other agents have said throughout the entire conversation. Think of it like a conference room meeting where everyone hears everything that's been discussed.

Sequential Orchestration: Agents operate in a linear pipeline where each agent only receives the output from the previous agent as input. Each agent processes this input independently without access to the broader conversation context.

Communication Pattern
GroupChatOrchestration with RoundRobin:

Agents take turns speaking into a shared conversation space

The RoundRobinGroupChatManager cycles through agents in order, but each agent responds to the entire conversation context

MaximumInvocationCount limits the total number of agent responses across all participants

Sequential Orchestration:

Agents pass their output directly to the next agent in the sequence

Each agent only sees the immediate previous agent's output

The flow is unidirectional and deterministic

Why the Confusion Arises
The confusion comes from observing the execution order in RoundRobin, which appears sequential. However, the underlying mechanics are completely different:
 
Conclusion
While both patterns can achieve similar outcomes in certain scenarios, they're architecturally distinct. GroupChatOrchestration with RoundRobin creates collaborative dialogue, while Sequential Orchestration creates processing pipelines. The choice between them should be based on whether you need shared collaborative context or structured data transformation.

Your exploration scenario actually benefited more from Sequential Orchestration because you needed a clear dependency chain (Environment → Safety → Motors) rather than collaborative discussion. The "silent agent" issues you experienced were exactly because GroupChatOrchestration wasn't the right architectural pattern for your pipeline-based workflow.
 */
