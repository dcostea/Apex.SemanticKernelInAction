using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

// Importing a plugin from a directory
var commandsPluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "CommandsPlugin");
kernel.ImportPluginFromPromptDirectory(commandsPluginPath, "commands_from_directory_plugin");

// Preparing the prompt for the semantic function
var prompt = """
    Your task is to break down complex commands into a sequence of basic moves such as {{$basic_moves}}.

    [COMPLEX COMMAND START]
    {{$input}}
    [COMPLEX COMMAND END]

    Provide only the sequence of the basic movements, without any additional explanations.

    Commands:
    """;
// Preparing the semantic function from plain text prompt (not fully packed with all settings such as argument types)
////var functionFromPrompt = kernel.CreateFunctionFromPrompt(prompt, functionName: "breakdown_complex_commands", description: "It breaks down the given complex command into a step-by-step sequence of basic moves.");
// Importing a plugin from a function list
////kernel.ImportPluginFromFunctions("commands_from_prompt_plugin", "Robot car commands plugin.", [ functionFromPrompt ]);

PrintAllPluginFunctions(kernel);


static void PrintAllPluginFunctions(Kernel kernel)
{
    Console.WriteLine("Registered plugins and functions and their parameters:");
    foreach (var plugin in kernel.Plugins)
    {
        Console.WriteLine($"  [{plugin.Name}] ({plugin.Description}) functions ({plugin.FunctionCount}):");
        foreach (var function in plugin.GetFunctionsMetadata())
        {
            Console.WriteLine($"    [{function.Name}] ({function.Description}) output parameter schema: {function.ReturnParameter.Schema}, input parameters:");
            foreach (var parameter in function.Parameters)
            {
                Console.WriteLine($"      [{parameter.Name}] schema: {parameter.Schema}");
            }
        }
    }
}
