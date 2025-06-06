using Microsoft.SemanticKernel;

namespace Filters;

public sealed class HumanInTheLoopFilter : IFunctionInvocationFilter
{
    private const int TimeoutSeconds = 3; // timeout time for reading a key from console

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ResetColor();
        Console.WriteLine($"  Function '{context.Function.Name}' is about to be invoked. Proceed ([y]/n)?");
        var yesNoResponse = ReadKeyWithTimeout();

        if (yesNoResponse == ConsoleKey.Y || yesNoResponse == ConsoleKey.Enter)
        {
            await next(context);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            var message = "  Command cancelled! Continue.";
            context.Result = new FunctionResult(context.Result, message);
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    private static ConsoleKey ReadKeyWithTimeout()
    {
        Task<ConsoleKeyInfo> readTask = Task.Run(() => Console.ReadKey(true));
        return readTask.Wait(TimeoutSeconds * 1000) 
            ? readTask.Result.Key 
            : ConsoleKey.Enter;
    }
}
