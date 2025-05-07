using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint"]!,
    apiKey: configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    modelId: configuration["OpenAI:ModelId"]!,
//    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

//var logger = kernel.Services.GetRequiredService<ILogger<Program>>();

var left = kernel.Plugins.GetFunction("MotorsPlugin", "turn_left");
var right = kernel.Plugins.GetFunction("MotorsPlugin", "turn_right");
KernelFunction[] subset = [left, right];

var executionSettings = new AzureOpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.None(),
    //FunctionChoiceBehavior = FunctionChoiceBehavior.None([]),
    //FunctionChoiceBehavior = FunctionChoiceBehavior.None(subset),
};

var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    """
);
history.AddUserMessage("""
    Your task is to break down complex commands into a sequence of these basic moves: forward, backward, turn left, turn right, and stop.
    
    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path.".
    
    Respond only with the moves, without any additional explanations.
    """);

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

Console.WriteLine($"RESPONSE: {response}");
Helpers.Printing.PrintTools(history);
