using LLama;
using LLama.Common;
using LLama.Native;
using LLamaSharp.SemanticKernel;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

//https://huggingface.co/Qwen/Qwen3-14B-GGUF
//const string ModelPath = @"c:\Temp\LLMs\GGUF\Qwen\Qwen3-14B-Q4_K_M.gguf";
//const string ModelPath = @"c:\Temp\LLMs\GGUF\Qwen\Qwen3-8B-Q4_K_M.gguf";
//const string ModelPath = @"c:\Temp\LLMs\GGUF\Qwen\Qwen3-8B-Q8_0.gguf";
const string ModelPath = @"c:\Temp\LLMs\GGUF\mistral\Mistral-Small-3.1-24B-Instruct-2503-Q4_K_M.gguf";

NativeLibraryConfig.All.WithLogCallback((level, message) =>
{
    if (level == LLamaLogLevel.Info || level == LLamaLogLevel.Debug || level == LLamaLogLevel.Continue) return;
    Console.WriteLine($"[{level}] {message.TrimEnd('\n')}");
});

var modelParams = new ModelParams(ModelPath)
{
    ContextSize = 1 << 12, // 4K context size 🦙
};
using var model = LLamaWeights.LoadFromFile(modelParams);
using var context = model.CreateContext(modelParams);
var executor = new InteractiveExecutor(context);
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatCompletionService>(new LLamaSharpChatCompletion(executor));
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
var kernel = builder.Build();

LLamaSharpPromptExecutionSettings executionSettings = new()
{
    Temperature = 0.1,
    TopP = 0.85,
    MaxTokens = 500
};

KernelArguments kernelArguments = new(executionSettings)
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
Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory = new(renderedSystemPrompt);
chatHistory.AddUserMessage($"{renderedUserPrompt}/no_think");

var chat = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("=== Chat Service Streaming with Chat History ===");

Console.Write($"RESPONSE: ");
StringBuilder response = new();
await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
{
    Console.Write(chunk.Content);
    response.Append(chunk.Content);
}
Console.WriteLine();

chatHistory.AddAssistantMessage(response.ToString());
var query = "Now, please tell me which were the first and the last steps in the json sequence?";
Console.WriteLine($"USER: {query}");
chatHistory.AddUserMessage($"{query}/no_think");
var response2 = await chat.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response2.Content}");
