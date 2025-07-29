using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using Plugins.Native;
using System.ComponentModel;

Console.WriteLine("Starting MCP Server...");

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

IKernelBuilder kernelBuilder = builder.Services.AddKernel();
kernelBuilder.Plugins.AddFromType<MotorsPlugin>();
//kernelBuilder.Plugins.AddFromType<SensorsPlugin>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]                     // marks the class for server discovery
public static class EchoTool
{
    [McpServerTool,                     // MCP attribute
     KernelFunction,                    // optional SK attribute (makes it an SK function too)
     Description("Echoes the provided message back to the caller.")]
    public static string Echo(string message) => $"echo: {message}";
}
