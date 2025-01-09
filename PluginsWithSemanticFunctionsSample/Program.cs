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

var commandsPluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "CommandsPlugin");
kernel.ImportPluginFromPromptDirectory(commandsPluginPath, "CommandsPlugin");

var prompt = """
    Your task is to break down complex commands into a sequence basic moves such as {{$basic_moves}}.
    Provide only the sequence of the basic movements, without any additional explanations.

    Complex command:
    {{$input}}.
    """;

// Preparing the prompt function from plain text (not fully packed with all settings)
var promptFunctionFromPrompt = kernel.CreateFunctionFromPrompt(prompt);
var functions = new List<KernelFunction> { promptFunctionFromPrompt };
kernel.ImportPluginFromFunctions("date_plugin", "Date plugin.", functions);

PrintAllPluginFunctions(kernel);


static void PrintAllPluginFunctions(Kernel kernel)
{
    Console.WriteLine("Registered plugins and functions and their parameters:");

    foreach (var plugin in kernel.Plugins)
    {
        Console.WriteLine($"\t[{plugin.Name}] ({plugin.Description}) functions ({plugin.FunctionCount}):");

        foreach (var function in plugin.GetFunctionsMetadata())
        {
            Console.WriteLine($"\t\t[{function.Name}] ({function.Description}) output parameter schema: {function.ReturnParameter.Schema}, input parameters:");

            foreach (var parameter in function.Parameters)
            {
                Console.WriteLine($"\t\t\t[{parameter.Name}] schema: {parameter.Schema}");
            }
        }
    }
}