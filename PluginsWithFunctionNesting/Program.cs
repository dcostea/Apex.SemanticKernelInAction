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

// Import the plugins
kernel.ImportPluginFromType<SensorsPlugin>(nameof(SensorsPlugin));
kernel.ImportPluginFromType<RainDetectorPlugin>(nameof(RainDetectorPlugin));
kernel.ImportPluginFromType<EnvironmentalMonitorPlugin>(nameof(EnvironmentalMonitorPlugin));

// Assess the rain threat
var rainResponse = kernel.InvokeAsync<string>(nameof(EnvironmentalMonitorPlugin), "assess_rain_threat");
Console.WriteLine(rainResponse.Result);

// Remove the RainDetectorPlugin and add the MotorsPlugin
kernel.Plugins.Remove(kernel.Plugins[nameof(RainDetectorPlugin)]);
kernel.Plugins.AddFromType<MotorsPlugin>();

// Now assess the fire threat
var fireResponse = kernel.InvokeAsync<string>(nameof(EnvironmentalMonitorPlugin), "assess_fire_threat");
Console.WriteLine(fireResponse.Result);


public class EnvironmentalMonitorPlugin
{
    [KernelFunction("assess_rain_threat")]
    public async Task<string> AssessRainThreat(Kernel kernel)
    {
        var arguments = new KernelArguments();
        var dropletLevel = await kernel.InvokeAsync<DropletLevel>(nameof(SensorsPlugin), "read_droplet_level");
        arguments["dropletLevel"] = dropletLevel;

        var wipperStatus = dropletLevel == DropletLevel.None
            ? await kernel.InvokeAsync<string>(nameof(RainDetectorPlugin), "stop_wipers") // wippers not needed
            : await kernel.InvokeAsync<string>(nameof(RainDetectorPlugin), "start_wipers", arguments); // wippers needed

        return $"""
            RAIN THREAT REPORT:
              Droplet level: {dropletLevel}
              Wipper status: {wipperStatus}
            """;
    }

    [KernelFunction("assess_fire_threat")]
    public async Task<string> AssessFireThreat(Kernel kernel)
    {
        var arguments = new KernelArguments();
        var tempReading = await kernel.InvokeAsync<int>(nameof(SensorsPlugin), "read_temperature");
        arguments["temperature"] = tempReading;

        var movementResult = await kernel.InvokeAsync<string>(nameof(MotorsPlugin), "forward", new() { ["distance"] = tempReading > 50 ? 2 : 10 });

        return $"""
            FIRE THREAT REPORT:
              Temperature: {tempReading} Celsius degrees
              Movement: {movementResult}
            """;
    }
}
