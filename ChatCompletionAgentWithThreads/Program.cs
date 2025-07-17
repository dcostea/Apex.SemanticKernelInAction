using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    configuration["AzureOpenAI:DeploymentName"]!,
//    configuration["AzureOpenAI:Endpoint"]!,
//    configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

#pragma warning disable SKEXP0001 // RetainArgumentTypes is experimental
ChatCompletionAgent agent = new()
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
    Arguments = new(new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions { RetainArgumentTypes = true }) })
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    }
};

ChatHistoryAgentThread agentThread = new();

do
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var query = Console.ReadLine();
    if (string.IsNullOrEmpty(query)) break;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Assistant > ");

    var kernelArguments = new KernelArguments()
    {
        ["query"] = query
        //e.g., There is a tree directly in front of the car. Avoid it and then come back to the original path.
    };

    Console.ForegroundColor = ConsoleColor.Green;
    await foreach (var response in agent.InvokeAsync(query, agentThread))
    {
        Console.WriteLine(response.Message.Content);
    }

    //Console.WriteLine("RESPONSE: ");
    //await foreach (var response in agent.InvokeStreamingAsync(query, agentThread))
    //{
    //    Console.Write(response.Message.Content);
    //}
    //Console.WriteLine();
}
while (true);

Console.WriteLine("CHAT HISTORY:");
foreach (var response in agentThread.ChatHistory)
{
    Console.WriteLine($"[{response.Role}] {response.Content}");
}
