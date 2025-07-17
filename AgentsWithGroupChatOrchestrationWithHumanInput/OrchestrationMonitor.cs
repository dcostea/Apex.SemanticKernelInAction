using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentsWithConcurrentOrchestration;

public class OrchestrationMonitor
{
    public bool IsApproved { get; private set; } = false;

    public ValueTask ResponseCallback(ChatMessageContent message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{message.AuthorName}] ");
        Console.ResetColor();
        Console.WriteLine($"{message.Content}");

        if (message.Role == AuthorRole.User) 
        {

        }

        if (message.AuthorName != "MotorsAgent")
        {
            IsApproved = message?.Content?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) == true
            && message.Content?.Contains("NOT APPROVED", StringComparison.InvariantCultureIgnoreCase) == false;
        }

        return ValueTask.CompletedTask;
    }

    internal ValueTask<ChatMessageContent> InteractiveCallback()
    {
        Console.WriteLine("\n# HUMAN INPUT (type APPROVED to approve): ");
        string? input = Console.ReadLine()?.ToUpper();

        if (string.IsNullOrWhiteSpace(input))
        {
            input = "The sequence is NOT APPROVED!";
        }

        ChatMessageContent userMessage = new(AuthorRole.User, input);
        //ChatMessageContent userMessage = new(AuthorRole.User, "The sequence is NOT APPROVED!");

        return ValueTask.FromResult(userMessage);
    }
}