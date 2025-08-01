﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Net.Http.Headers;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    configuration["AzureOpenAI:DeploymentName"]!,
//    configuration["AzureOpenAI:Endpoint"]!,
//    configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

// Import an Open API plugin from a URL.
var plugin = await kernel.CreatePluginFromOpenApiAsync(
    "RobotCarAPI",
    new Uri("https://localhost:7222/swagger/v1/swagger.json"),
    new OpenApiFunctionExecutionParameters 
    {
    AuthCallback = (request, parameters) =>
        {
            string token = "ROBOT_API_TOKEN";
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return Task.CompletedTask;
        }
    });

// Get the function to be invoked and its metadata and extension properties.
var function = plugin["forward"];
var kernelArguments = new KernelArguments { 
    ["distance"] = "10" 
};

var result = await kernel.InvokeAsync(function, kernelArguments);
Console.WriteLine($"RESULT: {result}");
