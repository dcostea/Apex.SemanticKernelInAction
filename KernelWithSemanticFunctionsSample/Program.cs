using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

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

var prompt = """
    Your task is to break down complex commands into a sequence basic moves such as {{$basic_moves}}.
    Provide only the sequence of the basic movements, without any additional explanations.

    Complex command:
    {{$input}}.
    """;

// Preparing the prompt function from plain text (not fully packed with all settings)
var promptFunctionFromPrompt = kernel.CreateFunctionFromPrompt(prompt);

// Preparing the prompt function
var promptTemplateConfig = new PromptTemplateConfig
{
    TemplateFormat = "semantic-kernel",
    Name = "BreakdownComplexCommands",
    Description = "It breaks down the given complex command into a step-by-step sequence of basic moves.",
    Template = prompt,
    InputVariables = 
    [
        new InputVariable { Name = "input", Description = "The complex command to be broken down.", IsRequired = true },
        new InputVariable { Name = "basic_moves", Description = "The basic moves that the robot car can perform.", IsRequired = true }
    ],
    OutputVariable = new OutputVariable 
    { 
        Description = "The sequence of basic moves that the robot car should perform to break down the complex command."
    },
    ExecutionSettings = { ["default"] = new OpenAIPromptExecutionSettings { MaxTokens = 1000, Temperature = 0.1 } }
};
var promptFunction = kernel.CreateFunctionFromPrompt(promptTemplateConfig); // create a prompt function from PromptTemplateConfig object with name, decription and execution settings

var kernelArguments = new KernelArguments()
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then resume the initial direction.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

#pragma warning disable SKEXP0001 // RenderedPrompt is experimental and it needs to be enabled explicitly

// Querying the prompt function
var promptResponse = await promptFunction.InvokeAsync(kernel, kernelArguments);
Console.WriteLine($"RENDERED PROMPT: {promptResponse.RenderedPrompt}"); // shows the rendered prompt of the prompt function
Console.WriteLine($"PROMPT RESPONSE: {promptResponse}");

var functionPromptRespone = promptResponse.Function;
Console.WriteLine($"PROMPT FUNCTION: {functionPromptRespone}");
Console.WriteLine($"\tName: {functionPromptRespone.Name}");
Console.WriteLine($"\tDescription: {functionPromptRespone.Description}");
Console.WriteLine($"\tPlugin name: {functionPromptRespone.PluginName}");
Console.WriteLine($"\tTemperature: {(functionPromptRespone.ExecutionSettings!["default"] as OpenAIPromptExecutionSettings)!.Temperature}");
Console.WriteLine($"\tInput variable: {string.Join("", functionPromptRespone.Metadata.Parameters.Select(p => $"\n\t\t{p.Name} : {p.ParameterType!.Name} {(p.IsRequired ? "required" : "")} ({p.Description})"))}");
Console.WriteLine($"\tOutput variable: {functionPromptRespone.Metadata.ReturnParameter.ParameterType} ({functionPromptRespone.Metadata.ReturnParameter.Description})");
