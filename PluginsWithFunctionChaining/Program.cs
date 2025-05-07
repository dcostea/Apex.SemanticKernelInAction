using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Plugins.Enums;
using Plugins.Native;

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

// Register all plugins
kernel.Plugins.AddFromType<SensorsPlugin>();
kernel.Plugins.AddFromType<RainDetectorPlugin>();

var arguments = new KernelArguments();
var dropletLevel = await kernel.InvokeAsync<DropletLevel>(nameof(SensorsPlugin), "read_droplet_level");
arguments["dropletLevel"] = dropletLevel;

var wipperStatus = dropletLevel == DropletLevel.None
    ? await kernel.InvokeAsync<string>(nameof(RainDetectorPlugin), "stop_wipers")
    : await kernel.InvokeAsync<string>(nameof(RainDetectorPlugin), "start_wipers", arguments);

Console.WriteLine($"""
    RAIN THREAT REPORT:
      Droplet level: {dropletLevel}
      Wipper status: {wipperStatus}
    """);


kernel.Plugins.Remove(kernel.Plugins[nameof(RainDetectorPlugin)]);
kernel.Plugins.AddFromType<MotorsPlugin>();

// Read the temperature
var tempReading = await kernel.InvokeAsync<int>(nameof(SensorsPlugin), "read_temperature");
arguments["temperature"] = tempReading;

// Move the robot car forward based on the temperature reading
var movementResult = await kernel.InvokeAsync<string>(nameof(MotorsPlugin), "forward", new() { ["distance"] = tempReading > 50 ? 2 : 10 });

// Report the result
Console.WriteLine($"""
    FIRE THREAT REPORT:
      Temperature: {tempReading} Celsius degrees
      Movement: {movementResult}
    """);
