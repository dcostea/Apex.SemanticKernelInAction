using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

var endpoint = new Uri(args.Length > 0 ? args[0] : "http://localhost:3001/mcp");
var transport = new HttpClientTransport(new()
{
    Endpoint = endpoint,
    Name = "Motors Client (HTTP)",
});

Console.WriteLine($"Connecting to {endpoint}");
await using var client = await McpClient.CreateAsync(transport);

Console.WriteLine("TOOLS AVAILABLE:");
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"  {tool.Name}: {tool.Description} {tool.JsonSchema}");
}

Console.WriteLine("\nPROMPTS AVAILABLE:");
var prompts = await client.ListPromptsAsync();
foreach (var prompt in prompts)
{
    Console.WriteLine($"  {prompt.Name}: {JsonSerializer.Serialize(prompt.ProtocolPrompt.Arguments)}");
}

Console.WriteLine("\nRESOURCES AVAILABLE:");
var resources = await client.ListResourcesAsync();
foreach (var resource in resources)
{
    Console.WriteLine($"  {resource.Name}: {resource.ProtocolResource.Uri}");
}

Console.WriteLine("\nRESOURCE TEMPLATES AVAILABLE:");
var resourceTemplates = await client.ListResourceTemplatesAsync();
foreach (var resourceTemplate in resourceTemplates)
{
    Console.WriteLine($"  {resourceTemplate.Name}: {resourceTemplate.ProtocolResourceTemplate.UriTemplate}");
}

var toolResult = await client.CallToolAsync("turn_left",
    new Dictionary<string, object?> { ["angle"] = 99 });
Console.WriteLine($"\nTOOL RESPONSE: {string.Join(Environment.NewLine, toolResult.Content.OfType<TextContentBlock>().Select(content => content.Text))}");

var simplePrompt = await client.GetPromptAsync("message_prompt");
var parametrizedPrompt = await client.GetPromptAsync("parametrized_message_prompt",
    new Dictionary<string, object?>
    {
        ["action"] = "There is a tree directly in front of the car. Avoid it and then return to the original path.",
    });
var simplePromptText = (TextContentBlock)simplePrompt.Messages.Single(message => message.Role == Role.User).Content;
var parametrizedPromptText = (TextContentBlock)parametrizedPrompt.Messages.Single(message => message.Role == Role.User).Content;
Console.WriteLine($"\nSIMPLE PROMPT RESPONSE: {simplePromptText.Text}");
Console.WriteLine($"PROMPT TEMPLATE RESPONSE: {parametrizedPromptText.Text}");

var bio = await client.ReadResourceAsync("resource://mcp/bio");
Console.WriteLine($"\nRESOURCE RESPONSE: {bio.Contents.OfType<TextResourceContents>().Single().Text}");
var greeting = await client.ReadResourceAsync("resource://mcp/greet/Robby");
Console.WriteLine($"TEMPLATE RESOURCE RESPONSE: {greeting.Contents.OfType<TextResourceContents>().Single().Text}");

// MCP tools can be invoked through Semantic Kernel even without a model.
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Plugins.AddFromFunctions("MCPTools", tools.Select(tool => tool.AsKernelFunction()));
var kernel = kernelBuilder.Build();
var kernelResult = await kernel.InvokeAsync("MCPTools", "stop");
Console.WriteLine($"\nSEMANTIC KERNEL TOOL RESPONSE: {kernelResult}");

Console.Write("\nRun the Semantic Kernel assistant? [y/N] ");
if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
{
    return;
}

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var deploymentName = configuration["AzureOpenAI:DeploymentName"];
var azureEndpoint = configuration["AzureOpenAI:Endpoint"];
var apiKey = configuration["AzureOpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(deploymentName) || string.IsNullOrWhiteSpace(azureEndpoint) || string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set AzureOpenAI:DeploymentName, AzureOpenAI:Endpoint, and AzureOpenAI:ApiKey in this project's user secrets before running the assistant.");
    Environment.ExitCode = 1;
    return;
}

kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, azureEndpoint, apiKey);
kernel = kernelBuilder.Build();

OpenAIPromptExecutionSettings executionSettings = new()
{
    MaxTokens = 1000,
    Temperature = 0.1F,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
};

var history = new ChatHistory("""
    You are an AI assistant controlling a simulated robot car.
    The permitted moves are forward, backward, turn left, turn right, and stop.
    Break complex commands down into basic moves and run the corresponding tools.
    Respond only with the moves and their parameters, without additional explanations.
    """);
history.AddUserMessage(parametrizedPromptText.Text);

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
Console.WriteLine($"\nASSISTANT RESPONSE: {response}");
