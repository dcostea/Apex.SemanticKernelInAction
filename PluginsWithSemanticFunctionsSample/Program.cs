using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
var kernel = builder.Build();

// Importing a plugin from a directory
var commandsPluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "CommandsPlugin");
kernel.ImportPluginFromPromptDirectory(commandsPluginPath, "commands_from_directory_plugin");

// Preparing the prompt for the semantic function
var functionPrompt = """
    Your task is to break down complex commands into a sequence of basic moves such as forward, backward, turn left, turn right, and stop.

    [COMPLEX COMMAND START]
    {{$input}}
    [COMPLEX COMMAND END]

    Provide only the sequence of the basic movements, without any additional explanations.

    Commands:
    """;

var anotherFunctionPrompt = """
    Your task is to extract the net trending direction from a complex command.
    The trending direction can be only one of the basic moves, such as forward, backward, turn left, or turn right.

    [COMPLEX COMMAND START]
    {{$input}}
    [COMPLEX COMMAND END]

    Provide only the trending direction, without any additional explanations.

    Trending direction:
    """;

// Preparing the semantic function from plain text prompt (not fully packed with all settings such as argument types)
var functionFromPrompt = kernel.CreateFunctionFromPrompt(functionPrompt,
    functionName: "breakdown_complex_commands_from_prompt",
    description: "It breaks down the given complex command into a step-by-step sequence of basic moves.");

var anotherFunctionFromPrompt = kernel.CreateFunctionFromPrompt(anotherFunctionPrompt,
    functionName: "extract_trending_direction_from_prompt",
    description: "It extracts the net trending direction from the given complex command.");

// Importing a plugin from a function list
kernel.ImportPluginFromFunctions("commands_from_prompt_plugin", "Robot car commands plugin.", [ functionFromPrompt, anotherFunctionFromPrompt ]);

// eventually we will see the functions from both plugins in the output
Helpers.Printing.PrintPluginsWithFunctions(kernel);

var kernelArguments = new KernelArguments
{
    //["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["input"] = "There is danger in front of the car. Run away!",
};


var response = await kernel.InvokeAsync(kernel.Plugins.GetFunction("commands_from_prompt_plugin", "breakdown_complex_commands_from_prompt"), kernelArguments);
Console.WriteLine($"RESPONSE: {response}");

var anotherResponse = await kernel.InvokeAsync(kernel.Plugins.GetFunction("commands_from_prompt_plugin", "extract_trending_direction_from_prompt"), kernelArguments);
Console.WriteLine($"RESPONSE: {anotherResponse}");

//var prompt = """
//    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
//    You have to break down the provided complex commands into basic moves you know.
//    Respond only with the moves, without any additional explanations.
    
//    {{breakdown_complex_commands 'There is a tree directly in front of the car. Avoid it and then come back to the original path.'}}        
//    """;

//var response = await kernel.InvokePromptAsync(prompt);
//Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}");
//Console.WriteLine($"RESPONSE: {response}");
