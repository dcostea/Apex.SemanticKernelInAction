using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Net;
using Polly;
using Polly.Extensions.Http;
using OpenAI.Chat;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
builder.Services.ConfigureHttpClientDefaults(c =>
{
    // Define a retry policy with exponential backoff
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    c.AddPolicyHandler(retryPolicy);
});
var kernel = builder.Build();

var prompt = """
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    Your task is to break down complex commands into a sequence of these basic moves.
    Provide only the sequence of the basic movements, without any additional explanations.

    There is a tree directly in front of the car. Avoid it and then resume the initial direction.
    """;

// the first argument of InvokePromptAsync is a prompt string, but it will be packed into a KernelFunction object by kernel just before sending it to the model.
var functionResult = await kernel.InvokePromptAsync(prompt);

var logger = kernel.Services.GetRequiredService<ILogger<Program>>();
logger.LogDebug(functionResult.ToString());

//var x = functionResult.Metadata["FinishReason"];

//Console.WriteLine(functionResult);

/*
//var response = functionResult.GetValue<OpenAIChatMessageContent>();

// properties like model id, role and content are the most relevant properties of the response
Console.WriteLine("RESPONSE");
Console.WriteLine($"  Model id: {response!.ModelId}");
Console.WriteLine($"  Role: {response!.Role}");
Console.WriteLine($"  Content: {response.Content}");

var metadata = functionResult.Metadata;

// other metadata properties may prove useful for debugging and monitoring
Console.WriteLine("METADATA");
Console.WriteLine($"  Created at: {metadata!["CreatedAt"]}");
Console.WriteLine($"  System fingerprint: {metadata!["SystemFingerprint"]}");
Console.WriteLine($"  Finish reason: {metadata!["FinishReason"]}");

// metadata property contains additional information about the response, like token usage, log probabilities, etc.
var tokenUsage = metadata!["Usage"] as ChatTokenUsage;
Console.WriteLine($"  Total token count: {tokenUsage!.TotalTokenCount} (input: {tokenUsage!.InputTokenCount}, output: {tokenUsage!.OutputTokenCount})");

*/
