using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Diagnostics;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins.Native;
using Filters;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

//var appBuilder = Host.CreateApplicationBuilder();
//appBuilder.AddServiceDefaults();
//var app = appBuilder.Build();
//app.Start();

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("FiltersWithTelemetry")
    .AddTelemetrySdk();
    //.AddEnvironmentVariableDetector();

var endpoint = new Uri(Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL") ?? "http://localhost:19002");
// Enable model diagnostics with sensitive data.
AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

// Make sure to specify the correct protocol
using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("Microsoft.SemanticKernel*")
    .AddConsoleExporter()
    //.AddOtlpExporter(o =>
    //{
    //    o.Endpoint = endpoint;
    //    //o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf; // Explicitly set protocol
    //    //o.Headers = $"Authorization=Bearer {token}";
    //    //o.HttpClientFactory = () =>
    //    //{
    //    //    var httpClientHandler = new HttpClientHandler
    //    //    {
    //    //        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    //    //    };
    //    //    return new HttpClient(httpClientHandler);
    //    //};
    //})
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter("Microsoft.SemanticKernel*")
    .AddConsoleExporter()
    //.AddOtlpExporter(o =>
    //{
    //    o.Endpoint = endpoint;
    //    //o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf; // Explicitly set protocol
    //    ////o.Headers = $"Authorization=Bearer {token}";
    //    //o.HttpClientFactory = () =>
    //    //{
    //    //    var httpClientHandler = new HttpClientHandler
    //    //    {
    //    //        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    //    //    };
    //    //    return new HttpClient(httpClientHandler);
    //    //};
    //})
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(o =>
    {
        o.SetResourceBuilder(resourceBuilder);
        o.AddConsoleExporter();
        //o.AddOtlpExporter(o =>
        //{
        //    o.Endpoint = endpoint;
        //    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        //    //o.Headers = $"Authorization=Bearer {token}";
        //    o.HttpClientFactory = () =>
        //    {
        //        var httpClientHandler = new HttpClientHandler
        //        {
        //            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        //        };
        //        return new HttpClient(httpClientHandler);
        //    };
        //});
        // Format log messages. This is default to false.
        o.IncludeFormattedMessage = true;
        o.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
builder.Services.AddSingleton(loggerFactory);
builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionFilter>();
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();
// Add a system message to define the AI's role and permitted actions
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
    """
);

// Add a user message with specific instructions for the robot car
history.AddUserMessage("""
    Perform these steps:
      {{forward 100}}
      {{backward 100}}
      {{stop}}
            
    Respond only with the moves, without any additional explanations.
    """
);

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response}");