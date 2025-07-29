using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// 1. Build MCP client (stdio variant shown here)
await using var mcp = await McpClientFactory.CreateAsync(
        new StdioClientTransport(new()
        {
            Name = "Motors",
            Command = @"..\..\..\..\McpServerSample\bin\Debug\net9.0\16.01 McpServerSample.exe"
        }));

// 2. Download tool metadata
var tools = await mcp.ListToolsAsync();

// 3. Build Kernel with OpenAI + MCP tools
var builder = Kernel.CreateBuilder();

builder.Services.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!);

builder.Plugins.AddFromFunctions("Motors", tools.Select(t => t.AsKernelFunction()));

var kernel = builder.Build();
