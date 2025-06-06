using Microsoft.SemanticKernel;

namespace Filters;

public sealed class MissingArgumentFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.Red;

        if (context.Function.Name.Equals("forward", StringComparison.InvariantCultureIgnoreCase) 
            || context.Function.Name.Equals("backward", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!context.Arguments.TryGetValue("distance", out var _))
            {
                int distance = 1;
                Console.WriteLine($"  Forcing 'distance' argument to {distance}");
                context.Arguments["distance"] = distance;
            }
        }

        if (context.Function.Name.Equals("turn_left", StringComparison.InvariantCultureIgnoreCase)
            || context.Function.Name.Equals("turn_right", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!context.Arguments.TryGetValue("angle", out var _))
            {
                int angle = 90;
                Console.WriteLine($"  Forcing 'angle' argument to {angle}");
                context.Arguments["angle"] = angle;
            }
        }

        Console.ResetColor();
        await next(context);
    }
}
