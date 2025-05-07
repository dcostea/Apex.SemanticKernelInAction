using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

var behaviorOptions = new FunctionChoiceBehaviorOptions
{
    AllowConcurrentInvocation = true,
    AllowParallelCalls = true
};

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: behaviorOptions, autoInvoke: false),
};

var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    """);
history.AddUserMessage("""
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.
    Use the tools you know to perform the moves.
    
    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    """);

var chat = kernel.GetRequiredService<IChatCompletionService>();

ChatMessageContent response;
IEnumerable<FunctionCallContent> functionCalls;

Console.ForegroundColor = ConsoleColor.Green;

do
{
    Console.WriteLine("-------http request---------------------");
    response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    history.Add(response);

    // get all function calls from the response
    functionCalls = FunctionCallContent.GetFunctionCalls(response);

    // Iterate through the assistant function calls and invoke them manually
    foreach (var functionCallContent in functionCalls)
    {
        // Manually call the function
        var functionCallResult = await functionCallContent.InvokeAsync(kernel);
        Console.WriteLine($"FUNC CALL: {functionCallContent.FunctionName} {string.Join(' ', functionCallContent.Arguments ?? [])}, FUNC RESP: {functionCallResult.Result}");

        // Manually add the function call result to the chat history
        history.Add(functionCallResult.ToChatMessage());
    }
}
while (functionCalls.Any());

Console.ResetColor();

// Resume processing by sending the updated history with the function call results back to the chat service
Console.WriteLine($"RESPONSE: {response}");
