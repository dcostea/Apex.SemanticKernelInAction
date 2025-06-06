﻿using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
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

var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    ChatSystemPrompt = """
        You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.
        """
});

var response = await kernel.InvokePromptAsync("""
    There is a tree directly in front of the car. Avoid it and then come back to the original path.
    """,
    kernelArguments);

Console.WriteLine(response);
