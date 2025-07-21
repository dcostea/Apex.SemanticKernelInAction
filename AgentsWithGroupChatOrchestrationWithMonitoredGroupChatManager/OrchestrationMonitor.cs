using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Plugins.Native;

namespace AgentsWithConcurrentOrchestration;

public class OrchestrationMonitor(Kernel kernel)
{
    public bool IsApproved { get; private set; } = false;

    public ValueTask ResponseCallback(ChatMessageContent message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{message.AuthorName}] ");
        Console.ResetColor();
        Console.WriteLine($"{message.Content}");

        // Check if the approval is from a user other than "MotorsAgent"
        if (!IsApproved && message.AuthorName != "MotorsAgent")
        {
            IsApproved = message.Content?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        if (IsApproved)
        {
            bool pluginExists = kernel.Plugins.TryGetPlugin(nameof(MotorsPlugin), out _);
            if (!pluginExists)
            {
                kernel.Plugins.AddFromType<MotorsPlugin>();

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("MotorsPlugin added to MotorsAgent");
                Console.ResetColor();
            }
        }

        return ValueTask.CompletedTask;
    }
}