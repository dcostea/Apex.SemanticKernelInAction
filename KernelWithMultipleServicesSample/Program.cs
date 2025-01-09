﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!,
    serviceId: "OPENAI");
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

#pragma warning disable SKEXP0001 // ServiceId property is experimental and it needs to be enabled explicitly

var kernelArguments = new KernelArguments(
    new OpenAIPromptExecutionSettings
    {
        ServiceId = "OPENAI"
    });

var prompt = """
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    Your task is to break down complex commands into a sequence of these basic moves.
    Provide only the sequence of the basic movements, without any additional explanations.

    There is a tree directly in front of the car. Avoid it and then resume the initial direction.
    """;

var response = await kernel.InvokePromptAsync(prompt, kernelArguments);
Console.WriteLine(response);
