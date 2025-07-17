using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Plugins.Native;

namespace AgentsWithConcurrentOrchestration;

public class OrchestrationMonitor1
{
    public bool Approved { get; private set; } = false;

    public bool Executed { get; private set; } = false;

    private readonly Kernel _kernel;
    private readonly ILogger _logger;

    public OrchestrationMonitor1(Kernel kernel, ILogger logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public ValueTask ResponseCallback(ChatMessageContent message)
    {
        if (message.Role != AuthorRole.Tool)
        {
            // Skiptool messages from printing to console
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{message.AuthorName}] ");
            Console.ResetColor();
            Console.WriteLine($"{message.Content}");

            _logger.LogDebug("[{messageAuthorName}] {messageContent}", message.AuthorName, message.Content);
        }
        else 
        {
            // Having tool messages means the function was executed
            Executed = true;
        }

        // Check if the NavigatorAgent has approved the route
        bool isNavigator = message.AuthorName == "NavigatorAgent";
        bool isApproved = message.Content?.Contains("APPROVED") == true;

        if (!isNavigator || !isApproved)
        {
            return ValueTask.CompletedTask;
        }

        // Mark the route as approved
        Approved = true;

        // Add MotorsPlugin to MotorsAgent if not already present
        bool pluginExists = _kernel.Plugins.TryGetPlugin(nameof(MotorsPlugin), out _);
        if (!pluginExists)
        {
            _kernel.Plugins.AddFromType<MotorsPlugin>();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("MotorsPlugin added to MotorsAgent");
            Console.ResetColor();

            _logger.LogDebug("MotorsPlugin added to MotorsAgent");
        }

        return ValueTask.CompletedTask;
    }
}
