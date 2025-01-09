using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

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

var prompt = """
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    Your task is to break down complex commands into a sequence of these basic moves.
    Provide only the sequence of the basic movements, without any additional explanations.

    There is a tree directly in front of the car. Avoid it and then resume the initial direction.
    """;

Console.WriteLine("STREAMING USING PROMPT TEMPLATE");

await foreach (var partialResponse in kernel.InvokePromptStreamingAsync(prompt))
{
    Console.Write(partialResponse);
}
Console.WriteLine();

var promptFunction = kernel.CreateFunctionFromPrompt(prompt);

Console.WriteLine("STREAMING USING PROMPT FUNCTION");

await foreach (var partialResponse in kernel.InvokeStreamingAsync(promptFunction))
{
    Console.Write(partialResponse);
}
Console.WriteLine();
