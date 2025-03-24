using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint"]!,
    apiKey: configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    modelId: configuration["OpenAI:ModelId"]!,
//    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
var kernel = builder.Build();

//var prompt = """
//    Your task is to break down complex commands into a sequence basic moves such as forward, backward, turn left, turn right, and stop.
//    You have to break down the provided complex commands into basic moves you know.
//    Respond only with the moves, without any additional explanations.

//    Complex command:
//    There is a tree directly in front of the car. Avoid it and then come back to the original path.
//    """;

var prompt = """
    Your task is to break down complex commands into a sequence for basic moves such as {{$basic_moves}}.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.

    Complex command:
    {{$input}}
    """;

// Preparing the prompt function from plain text (not fully packed with all settings)
var promptFunctionFromPromptText = kernel.CreateFunctionFromPrompt(prompt, functionName: "breaks_down_complex_command", description: "Breaks down a complex command to basic moves.");

Console.WriteLine($"""
    SEMANTIC FUNCTION:
      Name: {promptFunctionFromPromptText.Name}
      Description: '{promptFunctionFromPromptText.Description}'
      Input variable: {string.Join("", promptFunctionFromPromptText.Metadata.Parameters.Select(p => $"\n    {p.Name} : {p.ParameterType!.Name} {(p.IsRequired ? "required" : "")} '{p.Description}'"))}
      Output variable: {promptFunctionFromPromptText.Metadata.ReturnParameter.Schema} {promptFunctionFromPromptText.Metadata.ReturnParameter.ParameterType} '{promptFunctionFromPromptText.Metadata.ReturnParameter.Description}'
    """);

var kernelArguments = new KernelArguments()
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

//var response = await promptFunctionFromPromptText.InvokeAsync(kernel);
var response = await promptFunctionFromPromptText.InvokeAsync(kernel, kernelArguments);

Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}"); // shows the rendered prompt of the prompt function
Console.WriteLine($"PROMPT RESPONSE: {response}");
