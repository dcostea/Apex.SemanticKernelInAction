﻿using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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

        // If approved, add the MotorsPlugin (if it's not already added)
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

    internal ValueTask<ChatMessageContent> InteractiveCallback()
    {
        Console.WriteLine("\n# HUMAN INPUT (type APPROVED to approve): ");
        string? input = Console.ReadLine()?.ToUpper();

        if (string.IsNullOrWhiteSpace(input))
        {
            input = "The sequence is DENIED!";
        }

        ChatMessageContent userMessage = new(AuthorRole.User, input);

        // To hardcode a denial message, command the previous lines for reading the input, and uncomment the line below:
        //ChatMessageContent userMessage = new(AuthorRole.User, "The sequence is DENIED!");

        return ValueTask.FromResult(userMessage);
    }
}
