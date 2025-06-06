using Microsoft.SemanticKernel;

namespace Filters;

public sealed class FunctionVerboseFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(FunctionVerboseFilter)} invoking {context.Function.Name}");
        Console.ResetColor();
        await next(context);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(FunctionVerboseFilter)} invoked {context.Function.Name}");
        Console.ResetColor();
    }
}
