using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

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

var systemPrompt = """
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.
    """;

List<ChatMessageContent> messages = [
    new(AuthorRole.System, systemPrompt),
    new(AuthorRole.User, "Go 5 steps forward."),
    new(AuthorRole.Assistant, "forward, forward, forward, forward, forward"),
];
var history = new ChatHistory(messages); // initialize the chat history with some messages

var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

history.AddMessage(AuthorRole.User,
[
    new TextContent("What do you see in the image? If you see craters or big rocks, go back to the very first position."),
    //new ImageContent(new Uri("https://products.apexcode.ro/rover.jpeg"))
]);

var response = await chatCompletion.GetChatMessageContentAsync(history);
history.Add(response); // add the response to the chat history

Helpers.Printing.PrintHistory(history);