using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint"]!,
    apiKey: configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    modelId: configuration["OpenAI:ModelId"]!,
//    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
var kernel = builder.Build();

var prompt = """
    Your task is to break down complex commands into a sequence basic moves such as {{$basic_moves}}.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.

    Complex command:
    {{$input}}
    """;

// Preparing the prompt function
var promptTemplateConfig = new PromptTemplateConfig
{
    TemplateFormat = "semantic-kernel",
    Name = "breakdown_complex_commands",
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
var promptFunctionFromPromptTemplateConfig = kernel.CreateFunctionFromPrompt(promptTemplateConfig); // create a prompt function from PromptTemplateConfig object with name, description and execution settings

Console.WriteLine($"""
    SEMANTIC FUNCTION:
      Name: {promptFunctionFromPromptTemplateConfig.Name}
      Description: '{promptFunctionFromPromptTemplateConfig.Description}'
      Temperature: {(promptFunctionFromPromptTemplateConfig.ExecutionSettings["default"] as OpenAIPromptExecutionSettings)!.Temperature}
      Max tokens: {(promptFunctionFromPromptTemplateConfig.ExecutionSettings["default"] as OpenAIPromptExecutionSettings)!.MaxTokens}
      Input variable: {string.Join("", promptFunctionFromPromptTemplateConfig.Metadata.Parameters.Select(p => $"\n    {p.Name} : {p.ParameterType!.Name} {(p.IsRequired ? "required" : "")} '{p.Description}'"))}
      Output variable: {promptFunctionFromPromptTemplateConfig.Metadata.ReturnParameter.Schema} {promptFunctionFromPromptTemplateConfig.Metadata.ReturnParameter.ParameterType} '{promptFunctionFromPromptTemplateConfig.Metadata.ReturnParameter.Description}'
    """);

var kernelArguments = new KernelArguments()
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

// Querying the prompt function
var response = await promptFunctionFromPromptTemplateConfig.InvokeAsync(kernel, kernelArguments);

Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}"); // shows the rendered prompt of the prompt function
Console.WriteLine($"PROMPT RESPONSE: {response}");
