using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Diagnostics;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

//var logger = kernel.Services.GetRequiredService<ILogger<Program>>();

var behaviorOptions = new FunctionChoiceBehaviorOptions 
{
    AllowConcurrentInvocation = true,
    AllowParallelCalls = true
};

// uncomment to use a subset of functions for auto behavior
var left = kernel.Plugins.GetFunction("MotorsPlugin", "turn_left");
var right = kernel.Plugins.GetFunction("MotorsPlugin", "turn_right");
KernelFunction[] subset = [left, right];

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: behaviorOptions)
};

var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    """);
//history.AddUserMessage("""
//    Your task is to break down complex commands into a sequence of these basic moves: forward, backward, turn left, turn right, and stop.
//    Respond only with the moves, without any additional explanations.
//    Use the tools you know to perform the moves.

//    Complex command:
//    "Go in a shape of a square."
//    """);
history.AddUserMessage("""
    Your task is to break down complex commands into a sequence of these basic moves: forward, backward, turn left, turn right, and stop.
    Respond only with the moves, without any additional explanations.
    Use the tools you know to perform the moves.
    
    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    """);

var sw = new Stopwatch();
sw.Start();

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

sw.Stop();
Console.WriteLine($"RESPONSE (total time: {sw.Elapsed.TotalSeconds} seconds): {response}");
Helpers.Printing.PrintTools(history);

public sealed class StepsResult
{
    public List<string>? Steps { get; set; }
}