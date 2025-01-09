﻿using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Plugins.OpenApi;

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

#pragma warning disable SKEXP0040 // OpenApiFunctionExecutionParameters is experimental and it needs to be enabled explicitly

// Import an Open API plugin from a URL.
var plugin = await kernel.CreatePluginFromOpenApiAsync("WebApiSampleAPI",
    new Uri("https://localhost:7222/swagger/v1/swagger.json"),
    new OpenApiFunctionExecutionParameters());
;
// Get the function to be invoked and its metadata and extension properties.
var function = plugin["GetWeatherForecast"];
var functionResult = await kernel.InvokeAsync(function);
var result = functionResult.GetValue<RestApiOperationResponse>();
Console.WriteLine($"Function execution result: {result?.Content}");

//ImportPluginFromGrpc