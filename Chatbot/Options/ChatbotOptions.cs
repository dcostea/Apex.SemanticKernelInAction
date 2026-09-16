using System.ComponentModel.DataAnnotations;

namespace Chatbot.Options;

public sealed class ChatbotOptions
{
    [Range(0, 2)]
    public double Temperature { get; init; } = 0.1;

    [Required]
    public string SystemPrompt { get; init; } = """
        You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.
        """;

    [Required]
    [MinLength(1)]
    public string[] ExitCommands { get; init; } = ["exit", "quit"];
}
