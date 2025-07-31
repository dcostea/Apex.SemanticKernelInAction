using Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Orchestration;

public class OrchestrationMonitor(ILogger logger)
{
    public ValueTask ResponseCallback(ChatMessageContent message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{message.AuthorName}]");
        logger.LogDebug("[{messageAuthorName}] {messageContent}", message.AuthorName, message.Content);

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            if (message.Role == AuthorRole.Tool)
            {
                var toolCallResults = message.Items.OfType<FunctionResultContent>();
                foreach (var toolCallResult in toolCallResults)
                {
                    ColoredConsole.WriteLine($"  - ToolResult: {toolCallResult.PluginName}-{toolCallResult.FunctionName} [{toolCallResult.Result}]");
                }
            }
            else
            {
                ColoredConsole.WriteLine($"  - Content: {message.Content} ");
            }
        }
        else 
        {
            if (message.Items.Any(item => item is FunctionCallContent))
            {
                var toolCalls = message.Items.OfType<FunctionCallContent>();
                foreach (var toolCall in toolCalls)
                {
                    ColoredConsole.WriteLine($"  - ToolCall: {toolCall.PluginName}-{toolCall.FunctionName} {string.Join(" ", toolCall.Arguments ?? [])}");
                }
            }
            else 
            {
                ColoredConsole.WriteLine($"This {message.Role} message had no content and no tools!!!", ConsoleColor.Red);
            }
        }

        Console.ResetColor();

        return ValueTask.CompletedTask;
    }
}
