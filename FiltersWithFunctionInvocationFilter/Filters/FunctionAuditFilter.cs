using Microsoft.SemanticKernel;

namespace Filters;

public sealed class FunctionAuditFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(FunctionAuditFilter)} invoking {context.Function.Name}");
        Console.ResetColor();
        await next(context);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(FunctionAuditFilter)} invoked {context.Function.Name}");
        Console.ResetColor();
    }
}
