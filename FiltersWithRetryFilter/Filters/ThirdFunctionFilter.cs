using Microsoft.SemanticKernel;

namespace Filters;

public sealed class ThirdFunctionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(ThirdFunctionFilter)} invoking {context.Function.Name}");
        Console.ResetColor();
        await next(context);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(ThirdFunctionFilter)} invoked {context.Function.Name}");
        Console.ResetColor();
    }
}
