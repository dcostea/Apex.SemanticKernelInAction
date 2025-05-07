using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Helpers;

public static class Printing
{
    public static void PrintCompactTools(ChatHistory history)
    {
        Console.ForegroundColor = ConsoleColor.Green;

        foreach (var message in history)
        {
            if (message.Role == AuthorRole.Assistant)
            {
                var toolCalls = (message as OpenAIChatMessageContent)!.ToolCalls;
                foreach (var toolCall in toolCalls)
                {
                    var toolCallResponse = history?
                        .SelectMany(h => h.Items.OfType<FunctionResultContent>())?
                        .Where(x => x.CallId == toolCall.Id).FirstOrDefault();

                    Console.WriteLine($"FUNC CALL: {toolCall.FunctionName} {toolCall.FunctionArguments}, RESPONSE: {toolCallResponse?.Result}");
                }
            }
        }
        Console.ResetColor();
    }

    public static void PrintTools(ChatHistory history)
    {
        Console.ForegroundColor = ConsoleColor.Green;

        foreach (var message in history)
        {
            if (message.Role == AuthorRole.Assistant)
            {
                var messageId = (message.InnerContent as OpenAI.Chat.ChatCompletion)?.Id;
                var shortId = messageId?[^5..];
                var toolCalls = (message as OpenAIChatMessageContent)!.ToolCalls;
                foreach (var toolCall in toolCalls)
                {
                    Console.WriteLine($"FUNC CALL [call_{toolCall.Id[^5..]}:asst_{shortId}]: {toolCall.FunctionName} {toolCall.FunctionArguments}");
                }
            }
            if (message.Role == AuthorRole.Tool)
            {
                // get the functions result content of the current message
                var functionResult = message.Items.OfType<FunctionResultContent>().FirstOrDefault();
                Console.WriteLine($"FUNC RESP [call_{functionResult?.CallId?[^5..]}]: {message.Content}");
            }
        }
        Console.ResetColor();
    }

    public static void PrintHistory(ChatHistory history)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Chat history: ");
        foreach (var message in history)
        {
            Console.WriteLine($"ROLE: {message.Role}, {message.Content}");
        }
        Console.ResetColor();
    }

    public static void PrintPluginsWithFunctions(Kernel kernel)
    {
        Console.WriteLine("Kernel plugins and functions and their parameters:");
        foreach (var plugin in kernel.Plugins)
        {
            Console.WriteLine($"  {plugin.Name}: {plugin.Description} ({plugin.FunctionCount}):");

            foreach (var function in plugin.GetFunctionsMetadata())
            {
                Console.WriteLine($"    {function.Name}: {function.Description} | output parameter schema: {function.ReturnParameter.Schema}, input parameters:");

                foreach (var parameter in function.Parameters)
                {
                    Console.WriteLine($"      {parameter.Name}: {parameter.Schema}");
                }
            }
        }
    }
}
