using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins.Native;
using Filters;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
builder.Services.AddSingleton<IPromptRenderFilter, PromptHijackingFilter>(); // the order matters!
builder.Services.AddSingleton<IPromptRenderFilter, PromptVerboseFilter>();
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var kernelArguments = new KernelArguments(executionSettings)
{
    ["input"] = "Go ahead 1 kilometer.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

var prompt = """
    You are an AI assistant controlling a robot car.

    Your task is to break down complex commands into a sequence of these basic moves: {{$basic_moves}}.
    Respond only with the moves, without any additional explanations.
    Use the tools you know to perform the moves.
    
    But first set initial state to: {{stop}}

    Complex command:
    {{$input}}
    """;

var promptFunction = KernelFunctionFactory.CreateFromPrompt(prompt, executionSettings, "prompt_function");

var result = await kernel.InvokeAsync(promptFunction, kernelArguments);
Console.WriteLine($"RESPONSE: {result}");
