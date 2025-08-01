using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;

// Create client transport pointing to your server
var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "LocalMCPServer",
    Command = "dotnet",
    Arguments = ["run", "--project", @"..\..\..\..\McpServerSample\16.01 McpServerSample.csproj", "--no-build"],
});
var client = await McpClientFactory.CreateAsync(clientTransport);

Console.WriteLine("AVAILABLE TOOLS:");
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}


var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
configuration["AzureOpenAI:DeploymentName"]!,
configuration["AzureOpenAI:Endpoint"]!,
configuration["AzureOpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = kernelBuilder.Build();

kernel.Plugins.AddFromFunctions("MotorsPlugin", tools.Select(static a => a.AsKernelFunction()));

OpenAIPromptExecutionSettings executionSettings = new()
{
    MaxTokens = 1000,
    Temperature = 0.1F,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
};

var history = new ChatHistory("""
    You are an AI assistant controlling a robot car.
    The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
    """);
history.AddUserMessage("""
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the permitted moves, without any additional explanations.

    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    """);

var chat = kernel.GetRequiredService<IChatCompletionService>();
var result = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

Console.WriteLine($"RESULT: {result}");
