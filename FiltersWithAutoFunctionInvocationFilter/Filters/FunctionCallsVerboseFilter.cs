using Microsoft.SemanticKernel;

namespace Filters;

public sealed class AutoFunctionCallsVerboseFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        var functionCalls = FunctionCallContent.GetFunctionCalls(context.ChatHistory.Last()).ToArray();

        if (functionCalls is { Length: > 0 })
        {
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var functionCall in functionCalls)
            {
                Console.WriteLine($"  Request #{context.RequestSequenceIndex} invoking {functionCall.FunctionName}.");
            }
            Console.ResetColor();
        }

        // Example: get request sequence index
        Console.WriteLine($"Request sequence index: {context.RequestSequenceIndex}");

        // Example: get function sequence index
        Console.WriteLine($"Function sequence index: {context.FunctionSequenceIndex}");

        // Example: get total number of functions which will be called
        Console.WriteLine($"Total number of functions: {context.FunctionCount}");

        await next(context);

        context.Terminate = false;
        //Console.WriteLine("Filter terminate.");
    }
}
