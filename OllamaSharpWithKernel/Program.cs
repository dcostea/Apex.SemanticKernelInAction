using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Plugins.Native;

const string ModelUri = "http://localhost:11434";
const string Model = "llama3.1:latest";

var kernel = Kernel.CreateBuilder()
    //.AddOllamaTextGeneration(
    //    modelId: ollamaModelAlias,
    //    endpoint: new Uri(ModelUri))
    .AddOllamaChatCompletion(
        modelId: Model,
        endpoint: new Uri(ModelUri))
    .Build();

kernel.ImportPluginFromType<MotorsPlugin>();

var prompt = """
    Your task is to break down complex commands into a sequence for basic moves such as {{$basic_moves}}.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the permitted moves, without any additional explanations.
    
    Complex command:
    "{{$input}}"
    """;

//// InvokePromptStreamingAsync /////////////////////////////////////
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

Console.WriteLine($"RESPONSE: "); 
await foreach (string text in kernel.InvokePromptStreamingAsync<string>(prompt, kernelArguments))
{
    Console.Write(text);
}
Console.WriteLine();

//// ChatHistory //////////////////////////////////

var chatHistory = new ChatHistory("You are an AI assistant controlling a robot car.");
chatHistory.AddUserMessage(prompt);

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response}");

/// StreamingChatMessageContentsAsync /////////////////////////////////////

Console.WriteLine($"RESPONSE: ");
await foreach (var chatUpdate in chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
{
    if (chatUpdate.Role == AuthorRole.User)
    {
        Console.Write($"{chatUpdate.Role.Value}: {chatUpdate.Content}");
    }

    if (chatUpdate.Role == AuthorRole.Assistant)
    {
        Console.Write(chatUpdate.Content);
    }
}
