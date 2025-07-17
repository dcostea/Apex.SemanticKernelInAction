using Microsoft.SemanticKernel;

namespace AgentsWithGroupChatOrchestration;

public class OrchestrationMonitor
{
    public ValueTask ResponseCallback(ChatMessageContent message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{message.AuthorName}] ");
        Console.ResetColor();
        Console.WriteLine($"{message.Content}");

        return ValueTask.CompletedTask;
    }
}
