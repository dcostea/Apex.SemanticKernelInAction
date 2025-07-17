using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Plugins.Native;

namespace AgentsWithConcurrentOrchestration;

public class OrchestrationMonitor
{
    public bool Approved { get; private set; } = false;
    public bool Executed { get; private set; } = false;

    private readonly Kernel _kernel;
    public OrchestrationMonitor(Kernel kernel)
    {
        _kernel = kernel;
    }

    public ValueTask ResponseCallback(ChatMessageContent message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{message.AuthorName}] ");
        Console.ResetColor();
        Console.WriteLine($"{message.Content}");

        // If the message is from the tool, we can mark it as executed
        if (message.Role == AuthorRole.Tool)
        {
            Executed = true;
        }

        // Only the navigator or user can approve
        if (message.AuthorName == "MotorsAgent")
        {
            return ValueTask.CompletedTask;
        }

        // Check if the NavigatorAgent has approved the route
        bool isApproved = message.Content?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) == true 
            && message.Content?.Contains("NOT APPROVED", StringComparison.InvariantCultureIgnoreCase) == false;
        if (!isApproved)
        {
            // Not approved yet!
            return ValueTask.CompletedTask;
        }

        Approved = true;
        // Now is safe to equip the MotorsAgent with the MotorsPlugin!

        // Add MotorsPlugin to MotorsAgent if not already present
        bool pluginExists = _kernel.Plugins.TryGetPlugin(nameof(MotorsPlugin), out _);
        if (!pluginExists)
        {
            _kernel.Plugins.AddFromType<MotorsPlugin>();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("MotorsPlugin added to MotorsAgent");
            Console.ResetColor();
        }

        return ValueTask.CompletedTask;
    }
}
