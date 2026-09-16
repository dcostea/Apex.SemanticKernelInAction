using McpServerWithHttp.Plugins;
using McpServerWithHttp.Prompts;
using McpServerWithHttp.Resources;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var kernel = new Kernel();
kernel.Plugins.AddFromType<MotorsPlugin>();

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "MotorsServerWithHttp",
            Version = "1.0.0",
        };
        options.InitializationTimeout = TimeSpan.FromSeconds(10);
    })
    .WithHttpTransport(options => options.Stateless = true)
    .WithPrompts<MotorsPrompts>()
    .WithResources<MotorsResources>();

// Expose Semantic Kernel native functions as MCP tools, without an AI service.
foreach (var plugin in kernel.Plugins)
{
    foreach (var function in plugin)
    {
        builder.Services.AddSingleton(McpServerTool.Create(function, new McpServerToolCreateOptions
        {
            Name = function.Name,
        }));
    }
}

var app = builder.Build();
app.MapMcp("/mcp");
app.MapGet("/health", () => "ok");
await app.RunAsync();
