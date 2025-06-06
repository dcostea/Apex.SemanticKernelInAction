using LLama;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using LLama.Common;
using Microsoft.SemanticKernel.ChatCompletion;
using LLamaSharp.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;

//const string ModelPath = @"c:\Temp\LLMs\GGUF\DeepSeek-R1-Distill-Qwen-7B-Uncensored.gguf";
const string ModelPath = @"c:\Temp\LLMs\GGUF\Phi-3.5-mini-instruct_Uncensored-Q4_K_M.gguf";

var @params = new ModelParams(ModelPath)
{
    ContextSize = 1024, // Set the context size as needed
};

using var weights = LLamaWeights.LoadFromFile(@params);
using var context = new LLamaContext(weights, @params);
var ex = new InteractiveExecutor(context);
var chatGPT = new LLamaSharpChatCompletion(ex);
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatCompletionService>(sp => new LLamaSharpChatCompletion(ex, new LLamaSharpPromptExecutionSettings  { MaxTokens = -1, Temperature = 0, TopP = 0.1 }));
var kernel = builder.Build();

LLamaSharpPromptExecutionSettings executionSettings = new()
{
    Temperature = 0.4,
    TopP = 0.95F,
    MaxTokens = 1000,
};

var chat = kernel.GetRequiredService<IChatCompletionService>();

//LLama.Common.ChatHistory chatHistory = new();
//chatHistory.AddMessage(LLama.Common.AuthorRole.System, """
//    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
//    """);

//chatHistory.AddMessage(LLama.Common.AuthorRole.User, """
//    You have to break down the provided complex commands into basic moves you know.
//    Respond only with the permitted moves, without any additional explanations.

//    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
//    """);

var prompt = """
    # PERSONA
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.

    # ACTION
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the permitted moves, without any additional explanations.

    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    """;

//var response = await kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

await foreach (var token in kernel.InvokePromptStreamingAsync<string>(prompt, new KernelArguments(executionSettings)))
{
    Console.Write(token);
}

Console.WriteLine();
