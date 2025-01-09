using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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

// Preparing the method function from delegate
Func<string, string> datetimeDelegate = (string format) => DateTime.Now.ToString(format);

var methodDelegatedFunction = kernel.CreateFunctionFromMethod(
    method: datetimeDelegate,
    functionName: "GetCurrentDateTime",
    description: "Gets the current date and time for the specified format");

var methodDelegatedResponse = await methodDelegatedFunction.InvokeAsync(kernel, new KernelArguments { ["format"] = "R" });
Console.WriteLine($"DELEGATED METHOD RESPONSE: {methodDelegatedResponse}");
Console.WriteLine();

// Preparing the method function from reflection
var methodReflectedFunction = kernel.CreateFunctionFromMethod(
    method: typeof(DateTimeFunctions).GetMethod(nameof(DateTimeFunctions.GetCurrentDay))!,
    target: new DateTimeFunctions(),
    functionName: "GetCurrentDay",
    description: "Gets the current day."
);

var methodReflectedResponse = await methodReflectedFunction.InvokeAsync(kernel);
Console.WriteLine($"REFLECTED METHOD RESPONSE: {methodReflectedResponse}");

public class DateTimeFunctions
{
    public DayOfWeek GetCurrentDay() => DateTime.Now.DayOfWeek;
}
