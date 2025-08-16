using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Prompts;

[McpServerPromptType]
public static class MotorsPrompts
{
    [McpServerPrompt, Description("It breaks down the given complex command into a step-by-step sequence of basic moves.")]
    public static ChatMessage BreaksDownCommand([Description("The complex command to break down into a step-by-step sequence of basic moves.")] string input) =>
        new(ChatRole.User, $"""
        Your task is to break down complex commands into a sequence basic moves such as forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.

        Complex command:
        "{input}"
        """);
}

