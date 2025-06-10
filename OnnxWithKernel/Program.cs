using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Onnx;

//const string ModelPath = @"c:\Users\dcost\.foundry\cache\models\Microsoft\Phi-4-mini-reasoning-cuda-gpu\v1";
//const string ModelPath = @"c:\Users\dcost\.foundry\cache\models\Microsoft\mistralai-Mistral-7B-Instruct-v0-2-cuda-gpu\mistral-7b-instruct-v0.2-cuda-int4-rtn-block-32";
const string ModelPath = @"c:\Users\dcost\.foundry\cache\models\Microsoft\qwen2.5-14b-instruct-cuda-gpu\v3";

var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddOnnxRuntimeGenAIChatCompletion(
    modelPath: ModelPath,
    modelId: "onnx");
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

OnnxRuntimeGenAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0.7F,
    TopP = 0.8F,
    MaxTokens = 2000,
};

var kernelArguments = new KernelArguments(executionSettings)
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

    # TEMPLATE
    Respond only with the permitted moves, without any additional explanations. 
    Output format: JSON array of strings, e.g. { "steps": [ "basic_move1", "basic_move2", "basic_move3", ... ] }
    """;

Console.WriteLine("=== ONNX with Kernel and Invoke ===");

Console.Write($"RESPONSE: ");
await foreach (var chunk in kernel.InvokePromptStreamingAsync<string>($"{prompt}/no_think", kernelArguments))
{
    Console.Write(chunk);
}
Console.WriteLine();


// Cleanup resources
foreach (var target in kernel.GetAllServices<IChatCompletionService>().OfType<IDisposable>())
{
    target.Dispose();
}
