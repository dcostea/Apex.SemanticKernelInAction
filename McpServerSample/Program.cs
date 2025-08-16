using Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!);
kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = kernelBuilder.Build();

kernel.Plugins.AddFromType<MotorsPlugin>();
//kernel.Plugins.AddFromType<SensorsPlugin>();

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

builder.Services
    .AddMcpServer(opt =>
        {
            opt.ServerInfo = options.ServerInfo;
            opt.InitializationTimeout = options.InitializationTimeout;
        }
    )
    .WithStdioServerTransport()
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
        try
        {
            var mcpTool = McpServerTool.Create(function);
            builder.Services.AddSingleton(mcpTool);
            Console.WriteLine($"Registered MCP tool: {function.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to register MCP tool {function.Name}: {ex.Message}");
        }
    }
}

var app = builder.Build();

Console.WriteLine("MCP Server is running...");

await app.RunAsync();
