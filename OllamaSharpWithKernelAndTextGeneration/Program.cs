using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

const string ModelUri = "http://localhost:11434";
//const string Model = "mistral-small3.1:latest";
const string Model = "qwen3:14b";

var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddOllamaTextGeneration(
    modelId: Model,
    endpoint: new Uri(ModelUri));
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var executionSettings = new OllamaPromptExecutionSettings
{
    Temperature = 0.1F,
    TopP = 0.95F,
    NumPredict = 2000,
};

var kernelArguments = new KernelArguments(executionSettings)
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

var prompt = """
    You are an AI assistant controlling a robot car.

    Your task is to break down complex commands into a sequence for basic moves such as {{$basic_moves}}.
    
    Complex command:
    "{{$input}}"

    Respond only with the permitted moves, without any additional explanations.
    """;

Console.WriteLine("=== Kernel Invoke Streaming with Chat History ===");

Console.Write($"RESPONSE: ");
await foreach (string chunk in kernel.InvokePromptStreamingAsync<string>($"{prompt}/no_think", kernelArguments))
{
    Console.Write(chunk);
}
Console.WriteLine();
