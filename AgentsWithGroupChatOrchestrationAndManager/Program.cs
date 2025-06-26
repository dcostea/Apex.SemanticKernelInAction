using AgentsWithGroupChatOrchestrationAndManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

ChatCompletionAgent navigatorAgent = new()
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    LoggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>(),
    Kernel = kernel,
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
            You have to break down the provided complex commands into basic moves you know.
            Respond only with the permitted moves, without any additional explanations.
            """)),
    Arguments = new()
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    }
};

ChatCompletionAgent meteoAgent = new()
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    LoggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>(),
    Kernel = kernel,
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            You are an AI assistant responsible for meteo weather reports.
            Respond only from the provided context.
            """)),
    Arguments = new()
    {

    }
};

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

#pragma warning disable SKEXP0110
GroupChatOrchestration orchestration = new(
    new AIGroupChatManager(
        query,
        kernel.GetRequiredService<IChatCompletionService>())
    {
        MaximumInvocationCount = 5
    },
    navigatorAgent,
    meteoAgent)
{
    ResponseCallback = ResponseCallback,
    LoggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>(),
};

InProcessRuntime runtime = new();
await runtime.StartAsync();

Console.WriteLine($"\n# INPUT: {query}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(query, runtime);
_ = await result.GetValueAsync();

await runtime.RunUntilIdleAsync();


static ValueTask ResponseCallback(ChatMessageContent response)
{
#pragma warning disable SKEXP0001
    Console.WriteLine($"[{response.AuthorName}] {response.Content}");
    //History.Add(response);
    return ValueTask.CompletedTask;
}
