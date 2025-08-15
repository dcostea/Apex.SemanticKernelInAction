using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Plugins.Native;
using System.ComponentModel;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!);
kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = kernelBuilder.Build();

kernel.Plugins.AddFromType<MotorsPlugin>();

var builder = Host.CreateEmptyApplicationBuilder(null);

McpServerOptions options = new()
{
    ServerInfo = new Implementation
    {
        Name = "MotorsServer",
        Version = "1.0.0",
    },
    InitializationTimeout = TimeSpan.FromSeconds(10),
};

HttpServerTransportOptions httpTransportOptions = new()
{
    Endpoints = ["http://localhost:4554"]
};

builder.Services
    .AddMcpServer(opt =>
        {
            opt.ServerInfo = options.ServerInfo;
            opt.InitializationTimeout = options.InitializationTimeout;
        }
    )
    .WithStdioServerTransport()
    .WithHttpTransport()
    //.WithStreamServerTransport()  // Enables Streamed HTTP transport. Important: it needs to run in a webapi application!
    //.WithHttpTransport()          // Enables HTTP transport. Important: it needs to run in a webapi application!
    .WithPromptsFromAssembly()
    .WithResourcesFromAssembly()
    .WithToolsFromAssembly(); // scans the calling assembly for MCP tools

// Register the kernel plugin functions as mcp tools
foreach (var plugin in kernel.Plugins)
{
    foreach (var function in plugin)
    {
        var mcpTool = McpServerTool.Create(function);
        builder.Services.AddSingleton(mcpTool);
    }
}

var app = builder.Build();

Console.WriteLine("MCP Server is running...");

await app.RunAsync();


[McpServerToolType]
public static class SensorsTool
{
    [McpServerTool(Name = "read_temperature"), Description("Use thermal sensors to detect abnormal heat levels.")]
    public static async Task<int> ReadTemperature()
    {
        var random = new Random();
        var temperature = random.Next(-20, 100); // Simulate temperature reading
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] SENSORS: READING Temperature: {temperature} Celsius degrees.");
        return await Task.FromResult(temperature);
    }
}

[McpServerPromptType]
public static class MotorsPrompts
{
    [McpServerPrompt, Description("It breaks down the given complex command into a step-by-step sequence of basic moves.")]
    public static ChatMessage BreaksDownCommand([Description("The complex command to break down into a step-by-step sequence of basic moves.")] string input) =>
        new(ChatRole.User, $"""
        Your task is to break down complex commands into a sequence basic moves such as forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.

        Complex command:
        "{input}"
        """);
}
