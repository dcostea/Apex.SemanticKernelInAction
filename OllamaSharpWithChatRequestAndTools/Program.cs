using OllamaSharp;
using Tools;

const string ModelUri = "http://localhost:11434";
const string Model = "mistral-small3.1:latest";

var ollamaClient = new OllamaApiClient(new Uri(ModelUri), defaultModel: Model);

var chat = new Chat(ollamaClient);

var prompt = """
    You are an AI assistant controlling a robot car.

    Danger ahead! Run away.
    """;

Console.WriteLine("=== Ollama Client with Chat ===");

Console.Write($"RESPONSE: ");
await foreach (var chunk in chat.SendAsync(prompt, [new ForwardTool(), new BackwardTool(), new StopTool(), new TurnLeftTool(), new TurnRightTool()]))
{
    Console.Write(chunk);
}
Console.WriteLine();
