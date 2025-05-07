using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Diagnostics;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins.Native;
using Filters;

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
builder.Services.AddSingleton<IFunctionInvocationFilter, FirstFunctionFilter>();
builder.Services.AddSingleton<IFunctionInvocationFilter, SecondFunctionFilter>();
builder.Services.AddSingleton<IFunctionInvocationFilter, ThirdFunctionFilter>();
var kernel = builder.Build();

//kernel.FunctionInvocationFilters.Add(new FunctionVerboseFilter());

kernel.ImportPluginFromType<MotorsPlugin>();

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
};

var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    """);
history.AddUserMessage("""
    Perform these steps:
      {{forward 100}}
    
    Respond only with the moves, without any additional explanations.
    """);

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response}");
