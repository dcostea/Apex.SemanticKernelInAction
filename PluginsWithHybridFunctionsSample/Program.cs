using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
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

var nativeFunction = kernel.CreateFunctionFromMethod(
    typeof(DateTimeFunctions).GetMethod(nameof(DateTimeFunctions.GetCurrentDay))!,
    new DateTimeFunctions(),
    "GetCurrentDay",
    "Gets the current day."
);

var semanticFunction = kernel.CreateFunctionFromPrompt(prompt, functionName: "BreakdownComplexCommands", description: "It breaks down a complex command into basic commands.");
var functions = new List<KernelFunction> { semanticFunction, nativeFunction };
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

public class DateTimeFunctions
{
    public DayOfWeek GetCurrentDay() => DateTime.Now.DayOfWeek;
}


[Description("Date and time plugin.")]
public class DateTimePlugin
{
    [KernelFunction]
    [Description("Retrieves the current date in provided format.")]
    public static string GetCurrentDate([Description("Provided date format.")] string format = "d")
    {
        return DateTime.Now.ToString("D");
    }

    [KernelFunction]
    [Description("Retrieves the current time in provided format.")]
    public static string GetCurrentTime([Description("Provided time format.")] string format = "t")
    {
        return DateTime.Now.ToString("T");
    }
}
