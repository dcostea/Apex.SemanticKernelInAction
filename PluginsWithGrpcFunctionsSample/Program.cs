using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Plugins.Grpc;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

// Import an Open API plugin from a URL.
#pragma warning disable SKEXP0040 // Suppress the warning for evaluation purposes
var plugin = kernel.ImportPluginFromGrpcFile("robotcar.proto", "robot_car");
#pragma warning restore SKEXP0040

// Add arguments for required parameters, arguments for optional ones can be skipped.
var arguments = new KernelArguments
{
    ["address"] = "<gRPC-server-address>",
    ["payload"] = "<gRPC-request-message-as-json>"
};

// Run
var result = await kernel.InvokeAsync(plugin["<operation-name>"], arguments);

Console.WriteLine($"Plugin response: {result.GetValue<string>()}");
