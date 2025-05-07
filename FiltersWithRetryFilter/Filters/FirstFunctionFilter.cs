using Microsoft.SemanticKernel;

namespace Filters;

public sealed class FirstFunctionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(FirstFunctionFilter)}.invoking {context.Function.Name}");
        Console.ResetColor();
        await next(context);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(FirstFunctionFilter)} invoked {context.Function.Name}");
        Console.ResetColor();
    }
}
