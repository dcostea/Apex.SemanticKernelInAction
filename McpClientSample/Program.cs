using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;

var mcpServerProjectPath = @"..\..\..\..\McpServerSample\16.01 McpServerSample.csproj";    

var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "MotorsServer",
    Command = "dotnet",
    Arguments = ["run", "--project", mcpServerProjectPath, "--no-build"],
});

var client = await McpClientFactory.CreateAsync(clientTransport);

/// Using native functions exponsed by MCP Server
Console.WriteLine("AVAILABLE TOOLS:");
var tools = await client.ListToolsAsync();
if (tools == null)
{
    Console.WriteLine("Failed to retrieve tools from the MCP server.");
    return;
}
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
//kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = kernelBuilder.Build();

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

/// Using semantic functions (prompts) exponsed by MCP Server
Console.WriteLine("\nAVAILABLE PROMPTS:");
var prompts = await client.ListPromptsAsync();
foreach (var prompt in prompts)
{
    Console.WriteLine($"- {prompt.Name}: {prompt.Description}");
}

Console.WriteLine("\nINVOKING A REMOTE PROMPT:");

var arguments = new KernelArguments
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path."
};

var promptName = "breaks_down_command";
var myPrompt = await client.GetPromptAsync(promptName, arguments);
var promptMessages = myPrompt.ToChatMessages();

// 2. Invoke the prompt by name with the arguments
var promptResult = await chat.GetChatMessageContentAsync(promptMessages.Last().Text, executionSettings, kernel);

Console.WriteLine($"RESULT FROM '{promptName}': {promptResult}");
