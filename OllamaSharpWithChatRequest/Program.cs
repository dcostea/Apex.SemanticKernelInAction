using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

const string ModelUri = "http://localhost:11434";
const string Model = "mistral-small3.1:latest";

var ollamaClient = new OllamaApiClient(new Uri(ModelUri), defaultModel: Model);

Console.WriteLine("=== Ollama Client with Chat ===");

var request = new ChatRequest
{
    Messages =
    [
        new() {
            Role = ChatRole.System,
            Content = """
                You are an AI assistant controlling a robot car.
                Your task is to break down complex commands into a sequence for basic moves such as forward, backward, turn left, turn right, and stop.
                Respond only with the permitted moves, without any additional explanations.
                """
        },
        new() {
            Role = ChatRole.User,
            Content = """
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """
        }
    ],
    Stream = true,
    Options = new RequestOptions
    {
        Temperature = 0.1F,
        TopP = 0.95F,
        NumPredict = 2000,
    }
};

Console.Write($"RESPONSE: ");
await foreach (var chunk in ollamaClient.ChatAsync(request))
{
    Console.Write(chunk?.Message.Content);
}
Console.WriteLine();
