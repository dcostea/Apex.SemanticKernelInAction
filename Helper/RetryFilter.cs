using Microsoft.SemanticKernel;

namespace Helpers;

/*
    kernel.FunctionInvocationFilters.Add(new RetryFilter(3));
  */

public class RetryFilter : IFunctionInvocationFilter
{
    private readonly int _maxRetries;

    public RetryFilter(int maxRetries = 3)
    {
        _maxRetries = maxRetries;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        int attempts = 3;
        while (true)
        {
            try
            {
                await next(context);
                Console.WriteLine($"Attempt {3 - attempts + 1} succeeded. FUNC: {context.Function.Name} RESULT: {context.Result}");
                if (--attempts == 0) break;
            }
            catch (Exception) when (--attempts == 0)
            {
                attempts++;
                Console.WriteLine($"Attempt {3 - attempts + 1} failed. FUNC: {context.Function.Name} RESULT: {context.Result}");
            }
        }
    }
}
