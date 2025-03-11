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
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();


// Preparing the method function from delegate
Func<string> calibrateSensorsDelegate = () => 
{
    Console.WriteLine($"[{DateTime.Now:mm:ss}] CALIBRATING sensors...");
    return "All sensors have been calibrated.";
};

//// Create KernelFunction using KernelFunctionFactory
//var functionFromDelegatedMethod = KernelFunctionFactory.CreateFromMethod(
//    method: () => "All sensors have been calibrated.",
//    functionName: "calibrate_sensors",
//    description: "Calibrate robot car sensors.");

// Create KernelFunction using Kernel object
var functionFromDelegatedMethod = kernel.CreateFunctionFromMethod(
    method: calibrateSensorsDelegate,
    functionName: "calibrate_sensors",
    description: "Calibrate robot car sensors.");
var responseFromDelegatedMethod = await functionFromDelegatedMethod.InvokeAsync<string>(kernel);
Console.WriteLine($"DELEGATED METHOD RESPONSE: {responseFromDelegatedMethod}");
Console.WriteLine();


// Preparing the method function from reflection
var functionFromReflectedMethod = kernel.CreateFunctionFromMethod(
    method: typeof(SensorsFunctions).GetMethod(nameof(SensorsFunctions.ReadTemperature))!,
    target: new SensorsFunctions(),
    functionName: "read_temperature",
    description: "Reads environment temperature."
);
var responseFromReflectedMethod = await functionFromReflectedMethod.InvokeAsync<int>(kernel);
Console.WriteLine($"REFLECTED METHOD RESPONSE: {responseFromReflectedMethod}");

public class SensorsFunctions
{
    public static int ReadTemperature()
    {
        var random = new Random();
        var temperature = random.Next(-20, 100); // Simulate temperature reading
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Temperature: {temperature} Celsius degrees.");
        return temperature;
    }
}