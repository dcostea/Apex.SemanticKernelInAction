﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    configuration["AzureOpenAI:DeploymentName"]!,
//    configuration["AzureOpenAI:Endpoint"]!,
//    configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!,
    serviceId: "OPENAI");
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var kernelArguments = new KernelArguments(
    new OpenAIPromptExecutionSettings
    {
        ServiceId = "OPENAI"
    });

var prompt = """
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the permitted moves, without any additional explanations.
    
    There is a tree directly in front of the car. Avoid it and then come back to the original path.
    """;

var response = await kernel.InvokePromptAsync(prompt, kernelArguments);
Console.WriteLine(response);
