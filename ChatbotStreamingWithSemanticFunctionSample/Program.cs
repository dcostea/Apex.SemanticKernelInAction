﻿using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernel = Kernel.CreateBuilder()
//.AddAzureOpenAIChatCompletion(
//deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//endpoint: configuration["AzureOpenAI:Endpoint"]!,
//apiKey: configuration["AzureOpenAI:ApiKey"]!)
.AddOpenAIChatCompletion(
     modelId: configuration["OpenAI:ModelId"]!,
     apiKey: configuration["OpenAI:ApiKey"]!)
.Build();

kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "CommandsPlugin"), "CommandsPlugin");

var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    """
);

#pragma warning disable SKEXP0001 // FunctionChoiceBehavior is experimental and it needs to be enabled explicitly
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

while (true)
{
    Console.Write(" User >>> ");
    var prompt = Console.ReadLine(); // You have a tree in front of the car. Avoid it and then resume the initial direction.
    if (string.IsNullOrEmpty(prompt)) break;

    history.AddUserMessage(prompt);

    Console.Write($"  Bot >>> ");
    string fullMessage = string.Empty;
    await foreach (var chatUpdate in chat.GetStreamingChatMessageContentsAsync(history, executionSettings, kernel))
    {
        Console.Write(chatUpdate.Content);
        fullMessage += chatUpdate.Content;
    }
    Console.WriteLine();

    history.AddAssistantMessage(fullMessage);
}
