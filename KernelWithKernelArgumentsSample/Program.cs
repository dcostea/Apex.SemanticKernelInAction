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
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var promptTemplate = """
    You are an AI assistant controlling a robot car capable of performing basic moves: {{$movements}}.
    Your task is to break down complex commands into a sequence of these basic moves.
    Provide only the sequence of the basic movements, without any additional explanations.

    {{$input}}
    """;

var kernelArguments = new KernelArguments
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then resume the initial direction.",
    ["movements"] = "forward, backward, turn left, turn right, and stop"
};

var response = await kernel.InvokePromptAsync(promptTemplate, kernelArguments);

#pragma warning disable SKEXP0001 // RenderedPrompt is experimental and it needs to be enabled explicitly
Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}");
Console.WriteLine($"RESPONSE: {response.GetValue<string>()}");
