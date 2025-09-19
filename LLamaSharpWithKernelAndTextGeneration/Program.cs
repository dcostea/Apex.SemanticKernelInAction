using LLama;
using LLama.Common;
using LLama.Native;
using LLamaSharp.SemanticKernel;
using LLamaSharp.SemanticKernel.TextCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

//https://huggingface.co/Qwen/Qwen3-14B-GGUF
//const string ModelPath = @"c:\Temp\LLMs\GGUF\Qwen3\Qwen3-14B-Q4_K_M.gguf";
//const string ModelPath = @"c:\Temp\LLMs\GGUF\gemma\gemma-3-12b-it-Q5_K_M.gguf";
const string ModelPath = @"c:\Temp\LLMs\GGUF\mistral\Mistral-Small-3.1-24B-Instruct-2503-Q4_K_M.gguf";

NativeLibraryConfig.All.WithLogCallback((level, message) =>
{
    if (level == LLamaLogLevel.Info || level == LLamaLogLevel.Debug || level == LLamaLogLevel.Continue) return;
    Console.WriteLine($"[{level}] {message.TrimEnd('\n')}");
});

var modelParams = new ModelParams(ModelPath)
{
    ContextSize = 1 << 12, // 4K context size 🦙
    GpuLayerCount = -1
};
using var model = LLamaWeights.LoadFromFile(modelParams);
using var context = model.CreateContext(modelParams);
var executor = new StatelessExecutor(model, modelParams);
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<ITextGenerationService>(new LLamaSharpTextCompletion(executor));
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
var kernel = builder.Build();

var responseSchema = """
    {
        "type": "object",
        "properties": {
            "steps": {
                "type": "array",
                "items": {
                    "type": "string"
                }
            }
        },
        "required": ["steps"],
        "additionalProperties": false
    }
    """;

LLamaSharpPromptExecutionSettings executionSettings = new()
{
    Temperature = 0.1,
    TopP = 0.85,
    MaxTokens = 100,
    ResponseFormat = responseSchema,
};

KernelArguments kernelArguments = new(executionSettings)
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

var prompt = """
    # PERSONA
    You are an AI assistant controlling a robot car.

    # ACTION
    Your task is to break down complex commands into a sequence for basic moves such as {{$basic_moves}}.

    # CONTEXT
    Complex command:
    "{{$input}}"

    # OUTPUT TEMPLATE
    Respond only with the permitted moves, without any additional explanations. 
    Output format: JSON array of strings, e.g. { "steps": [ "basic_move1", "basic_move2", "basic_move3", ... ] }
    """;

Console.WriteLine("=== Kernel Invoke Streaming ===");

Console.Write($"RESPONSE: ");
await foreach (var chunk in kernel.InvokePromptStreamingAsync<string>(prompt, kernelArguments))
{
    Console.Write(chunk);
}
Console.WriteLine();
