using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Onnx;
using System.Text;

const string ModelPath = @"c:\Temp\LLMs\ONNX\phi-3.5-mini-instruct\cpu_and_mobile\cpu-int4-awq-block-128-acc-level-4";
//const string ModelPath = @"c:\Temp\LLMs\ONNX\mistral-7b-instruct-v0.2-cuda-fp16";

var builder = Kernel.CreateBuilder();
builder.AddOnnxRuntimeGenAIChatCompletion(
    modelPath: ModelPath,
    modelId: "onnx");
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

OnnxRuntimeGenAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0.4F,
    TopP = 0.95F,
    MaxTokens = 5000,
};

var kernelArguments = new KernelArguments()
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

// Prepare the system and user prompts using the template factory
var systemPrompt = """
    You are an AI assistant controlling a robot car.
    Your task is to break down complex commands into a sequence for basic moves such as {{$basic_moves}}.
    Respond only with the permitted moves, without any additional explanations.
    Output format: JSON array of strings, e.g. { "steps": [ "basic_move1", "basic_move2", "basic_move3", ... ] }
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
chatHistory.AddUserMessage(renderedUserPrompt);

var chat = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("=== ONNX with Kernel and Chat History ===");

Console.Write($"RESPONSE: ");
StringBuilder response = new();
await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
{
    response.Append(chunk.Content);
    Console.Write(chunk.Content);
}
Console.WriteLine();
chatHistory.AddAssistantMessage(response.ToString());


var query = "Now, please tell me which were the first and the last steps in the json sequence?";
chatHistory.AddUserMessage(query);
Console.WriteLine($"USER: {query}");
var response2 = await chat.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response2.Content}");


// Cleanup resources
foreach (var target in kernel.GetAllServices<IChatCompletionService>().OfType<IDisposable>())
{
    target.Dispose();
}
