using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
configuration["AzureOpenAI:DeploymentName"]!,
configuration["AzureOpenAI:Endpoint"]!,
configuration["AzureOpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = kernelBuilder.Build();

kernel.Plugins.AddFromType<MotorsPlugin>();

var builder = Host.CreateEmptyApplicationBuilder(null);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

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
