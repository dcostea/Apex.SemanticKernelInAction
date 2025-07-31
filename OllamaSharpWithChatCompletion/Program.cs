using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp;

const string ModelUri = "http://localhost:11434";
const string Model = "mistral-small3.1:latest";
var ollamaClient = new OllamaApiClient(new Uri(ModelUri), defaultModel: Model);

var chat = ollamaClient.AsChatCompletionService();

var executionSettings = new OllamaPromptExecutionSettings
{
    Temperature = 0.1F,
    TopP = 0.95F,
    NumPredict = 2000,
};

var chatHistory = new ChatHistory("""
    You are an AI assistant controlling a robot car.
    Your task is to break down complex commands into a sequence for basic moves such as forward, backward, turn left, turn right, and stop.
    Respond only with the permitted moves, without any additional explanations.
    Output format: JSON array of strings, e.g. { "steps": [ "basic_move1", "basic_move2", "basic_move3", ... ] }
    """);
chatHistory.AddUserMessage("""
    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    """);

Console.WriteLine("=== Ollama Client with Chat and Tools ===");

Console.Write($"RESPONSE: ");
await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings))
{
    Console.Write(chunk.Content);
}
Console.WriteLine();
