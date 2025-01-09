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

//kernel.ImportPluginFromType<DateTimePlugin>("datetime_plugin");
kernel.ImportPluginFromObject(new DateTimePlugin(), "datetime_plugin");

// Preparing the method function from reflection
var methodReflectedFunction = kernel.CreateFunctionFromMethod(
    typeof(DateTimeFunctions).GetMethod(nameof(DateTimeFunctions.GetCurrentDay))!,
    new DateTimeFunctions(),
    "GetCurrentDay",
    "Gets the current day."
);
var functions = new List<KernelFunction> { methodReflectedFunction };
kernel.ImportPluginFromFunctions("date_plugin", "Date plugin.", functions);


PrintAllPluginFunctions(kernel);


static void PrintAllPluginFunctions(Kernel kernel)
{
    Console.WriteLine("Registered plugins and functions and their parameters:");

    foreach (var plugin in kernel.Plugins)
    {
        Console.WriteLine($"\t{plugin.Name}: {plugin.Description} ({plugin.FunctionCount}):");

        foreach (var function in plugin.GetFunctionsMetadata())
        {
            Console.WriteLine($"\t\t{function.Name}: {function.Description} | output: {function.ReturnParameter.Schema}, input parameters:");

            foreach(var parameter in function.Parameters)
            {
                Console.WriteLine($"\t\t\t{parameter.Name}: {parameter.Schema}");
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

