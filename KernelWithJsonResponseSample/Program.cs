using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
var kernel = builder.Build();

var logger = kernel.Services.GetRequiredService<ILogger<Program>>();

#pragma warning disable SKEXP0010 // ResponseFormat is experimental and it needs to be enabled explicitly
var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
{
    ResponseFormat = typeof(StepsResult), // model response can be formatted as json_object
    ChatSystemPrompt = """
        You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, left, and right.

        Your task is to break down complex commands into a sequence of these basic moves.
        Provide only the sequence of the basic movements, without any additional explanations.
        """ // the system prompt taylors the model behaviour
});

var response = await kernel.InvokePromptAsync("""
    There is a tree directly in front of the car. Avoid it and then resume the initial direction.
    """, // the user prompt which changes which each new query
    kernelArguments);

logger.LogDebug("RESPONSE: {response}", response);

Console.WriteLine("Steps: ");
var stepsResult = JsonSerializer.Deserialize<StepsResult>(response.GetValue<string>()!); // the response as structrured data
foreach (var step in stepsResult!.Steps!)
{
    Console.WriteLine($"  {step}");
}

public sealed class StepsResult
{
    public List<string>? Steps { get; set; }
}
