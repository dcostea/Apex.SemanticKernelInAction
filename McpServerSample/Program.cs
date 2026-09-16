using Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// These native plugin functions do not require an AI service or credentials.
var kernel = new Kernel();

kernel.Plugins.AddFromType<MotorsPlugin>();
//kernel.Plugins.AddFromType<SensorsPlugin>();

var builder = Host.CreateEmptyApplicationBuilder(null);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

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
            Console.Error.WriteLine($"Registered MCP tool: {function.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to register MCP tool {function.Name}: {ex.Message}");
        }
    }
}

var app = builder.Build();

Console.Error.WriteLine("MCP Server is running...");

await app.RunAsync();
