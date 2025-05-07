using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
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

var left = kernel.Plugins.GetFunction("MotorsPlugin", "turn_left");
var right = kernel.Plugins.GetFunction("MotorsPlugin", "turn_right");
KernelFunction[] subset = [left, right];

var executionSettings = new OpenAIPromptExecutionSettings
{
    // FunctionChoiceBehavior determines how the AI interacts with functions:
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), // AI can choose to call functions or respond with text
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([]), // AI can choose, but no functions are available
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(subset), // AI can choose, but only from the subset
    //FunctionChoiceBehavior = FunctionChoiceBehavior.Required(), // AI must call a function
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Required([]), // AI must call a function, but none are available
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Required(subset), // AI must call a function from the subset
     FunctionChoiceBehavior = FunctionChoiceBehavior.None(), // Function calling is disabled
    // FunctionChoiceBehavior = FunctionChoiceBehavior.None([]), // Function calling is disabled, no functions available
    // FunctionChoiceBehavior = FunctionChoiceBehavior.None(subset), // Function calling is disabled, subset ignored
    // FunctionChoiceBehavior = null, // No specific behavior is defined

};

var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
    """);
history.AddUserMessage("""
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.
    Use the tools you know to perform the moves.

    But first set initial state to: {{MotorsPlugin-stop}}
    
    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    """);

//Respond only with what tools would you call to perform the moves, without any additional explanations.

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

Console.WriteLine($"RESPONSE: {response}");
Helpers.Printing.PrintTools(history);
