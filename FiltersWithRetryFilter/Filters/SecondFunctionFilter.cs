using Microsoft.SemanticKernel;

namespace Filters;

public sealed class SecondFunctionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(SecondFunctionFilter)} invoking {context.Function.Name}");
        Console.ResetColor();
        await next(context);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(SecondFunctionFilter)} invoked {context.Function.Name}");
        Console.ResetColor();
    }
}
