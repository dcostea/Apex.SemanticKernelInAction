﻿using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.SemanticKernel.Plugins.Web;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    configuration["OpenAI:ModelId"]!,
//    configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
var kernel = builder.Build();

var bingConnector = new BingConnector(configuration["BingSearchKey"]!);
var bingPlugin = new WebSearchEnginePlugin(bingConnector);
kernel.ImportPluginFromObject(bingPlugin, "bing");

var promptTemplate = """
    Is it safe to drive?
    Give me a report in 20 words or less. Use metric system.

    ### real-time weather report
    {{search}}
    """;
var semanticFunction = kernel.CreateFunctionFromPrompt(promptTemplate);

var kernelArguments = new KernelArguments
{
    ["query"] = "What is the wind speed and direction in The Hague right now"
};

//var functioResult = await kernel.InvokeAsync("bing", "search", kernelArguments);

var response = await kernel.InvokeAsync(semanticFunction, kernelArguments);

Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}"); // shows the rendered prompt of the prompt function
Console.WriteLine($"PROMPT RESPONSE: {response}");
