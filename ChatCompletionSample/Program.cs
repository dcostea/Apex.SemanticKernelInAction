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
        Your task is to break down complex commands into a sequence of these basic moves.
        Provide only the sequence of the basic movements, without any additional explanations.
        """
});

var response = await kernel.InvokePromptAsync("""
    There is a tree directly in front of the car. Avoid it and then resume the initial direction.
    """,
    kernelArguments);

Console.WriteLine(response);
