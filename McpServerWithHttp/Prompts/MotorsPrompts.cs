using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServerWithHttp.Prompts;

[McpServerPromptType]
public class MotorsPrompts
{
    [McpServerPrompt(Name = "string_prompt"), Description("A robot car command as a string prompt without arguments.")]
    public static string StringPrompt() => """
        There is a tree directly in front of the car. Avoid it and then return to the original path.
        """;

    [McpServerPrompt(Name = "message_prompt"), Description("A robot car command as a user message without arguments.")]
    public static ChatMessage MessagePrompt() => new(ChatRole.User, StringPrompt());

    [McpServerPrompt(Name = "parametrized_message_prompt"), Description("A user message containing a complex robot car command.")]
    public static ChatMessage MessagePromptWithArguments(
        [Description("The complex action to be performed.")] string action) =>
        new(ChatRole.User, $"""
            Break the following complex command into basic robot car moves:
            {action}
            """);
}
