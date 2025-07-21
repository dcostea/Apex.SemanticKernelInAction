using Microsoft.SemanticKernel;

namespace AgentsWithGroupChatOrchestration;

public class OrchestrationMonitor
{
    public bool IsApproved { get; private set; } = false;

    public ValueTask ResponseCallback(ChatMessageContent message)
    {
        if (message.AuthorName != "MotorsAgent")
        {
            IsApproved = message.Content?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{message.AuthorName}] ");
        Console.ResetColor();
        Console.WriteLine($"{message.Content}");

        return ValueTask.CompletedTask;
    }
}
