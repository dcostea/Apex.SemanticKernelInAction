using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//endpoint: configuration["AzureOpenAI:Endpoint"]!,
//apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.ImportPluginFromObject(new SensorsPlugin("Robby"), "sensors_plugin");
//kernel.ImportPluginFromType<SensorsPlugin>("sensors_plugin");

// Preparing the method function from reflection
var nativeFunction = kernel.CreateFunctionFromMethod(
    typeof(MaintenanceFunctions).GetMethod(nameof(MaintenanceFunctions.CalibrateSensors))!,
    new MaintenanceFunctions(),
    "calibrate_sensors",
    "Calibrates all sensors on the robot car."
);
kernel.ImportPluginFromFunctions("maintenance_plugin", "Robot car maintenance plugin.", [nativeFunction]);

Helpers.Printing.PrintPluginsWithFunctions(kernel);

// The prompt calls calibrate_sensors, read_temperature and read_wind_speed functions from the kernel plugins
var prompt = """
    You are an AI assistant controlling a robot car.

    Temperature: {{read_temperature}}
    Wind speed: {{read_wind_speed}}
    Sensors status: {{calibrate_sensors}}

    Provide a report with the previous findings.
    """;

// Invoke the prompt with the kernel arguments and kernel plugin
var response = await kernel.InvokePromptAsync(prompt);
Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}");
Console.WriteLine($"RESPONSE: {response}");

public class MaintenanceFunctions
{
    public static async Task<string> CalibrateSensors()
    {
        Console.WriteLine($"[{DateTime.Now:mm:ss}] CALIBRATING sensors...");
        return await Task.FromResult("All sensors have been calibrated.");
    }
}

[Description("Robot car sensors plugin.")]
public class SensorsPlugin(string RobotName)
{
    public string RobotName { get; } = RobotName;

    [KernelFunction("read_temperature"), Description("Use thermal sensors to detect abnormal heat levels.")]
    public async Task<int> ReadTemperature()
    {
        var random = new Random();
        var temperature = random.Next(-20, 100); // Simulate temperature reading
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Temperature: {temperature} Celsius degrees.");
        return await Task.FromResult(temperature);
    }

    [KernelFunction("read_wind_speed"), Description("Reads and returns the wind speed in kmph.")]
    public async Task<int> ReadWindSpeed()
    {
        var random = new Random();
        var speed = random.Next(0, 100);
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Wind speed: {speed} kmph"); // Simulate wind speed reading
        return await Task.FromResult(speed);
    }
}