﻿using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernel = Kernel.CreateBuilder()
//.AddAzureOpenAIChatCompletion(
//configuration["AzureOpenAI:DeploymentName"]!,
//configuration["AzureOpenAI:Endpoint"]!,
//configuration["AzureOpenAI:ApiKey"]!)
.AddOpenAIChatCompletion(
     configuration["OpenAI:ModelId"]!,
     configuration["OpenAI:ApiKey"]!)
.Build();

kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "CommandsPlugin"), "CommandsPlugin");

var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    """
);

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

while (true)
{
    Console.Write(" User >>> ");
    var prompt = Console.ReadLine(); // There is a tree directly in front of the car. Avoid it and then come back to the original path.
    if (string.IsNullOrEmpty(prompt)) break;

    history.AddUserMessage(prompt);

    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

    Console.WriteLine($"  Bot >>> {response}");

    history.Add(response);
}
