using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Plugins.Native;

const string ModelUri = "http://localhost:11434";
//const string Model = "mistral-small3.1:latest";
//const string Model = "llama3.2:latest";
const string Model = "qwen3:14b";

var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddOllamaChatCompletion(
    modelId: Model,
    endpoint: new Uri(ModelUri))
.Build();
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

var executionSettings = new OllamaPromptExecutionSettings
{
    Temperature = 0.1F,
    TopP = 0.95F,
    NumPredict = 2000,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
};

var kernelArguments = new KernelArguments(executionSettings)
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

var systemPrompt = """
    You are an AI assistant controlling a robot car.
    Your task is to break down complex commands into a sequence for basic moves such as {{$basic_moves}}.
    Respond only with the permitted moves, without any additional explanations.
    """;
var userPrompt = """
    Complex command:
    "{{$input}}"
    """;
var promptTemplateFactory = new KernelPromptTemplateFactory();
var systemPromptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(systemPrompt));
var renderedSystemPrompt = await systemPromptTemplate.RenderAsync(kernel, kernelArguments);
var userPromptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(userPrompt));
var renderedUserPrompt = await userPromptTemplate.RenderAsync(kernel, kernelArguments);
ChatHistory chatHistory = new(renderedSystemPrompt);
chatHistory.AddUserMessage($"{renderedUserPrompt}/no_think");

var chat = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("=== Chat Service with Chat History ===");

var response = await chat.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response}");

//Console.Write($"RESPONSE: ");
//await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
//{
//    Console.Write(chunk.Content);
//}
//Console.WriteLine();
