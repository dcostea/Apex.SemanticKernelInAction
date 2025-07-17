﻿using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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

var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    ChatSystemPrompt = """
        You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.
        """
});

while (true)
{
    Console.Write(" User >>> ");
    var prompt = Console.ReadLine(); // e.g. "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    if (string.IsNullOrEmpty(prompt)) break;

    var response = await kernel.InvokePromptAsync(prompt, kernelArguments);

    Console.WriteLine($"  Bot >>> {response}");
}
