using Microsoft.SemanticKernel;

namespace Filters;

public sealed class BackwardConfirmationFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Function.Name == "backward")
        {
            string message;

            Console.WriteLine($"  Moving backward is cowardly, are you sure ([y]/n)?");
            var yesNoResponse = Console.ReadKey(true);
            if (yesNoResponse.Key == ConsoleKey.Y || yesNoResponse.Key == ConsoleKey.Enter)
            {
                await next(context);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                message = "  Moving backward cancelled! Continue.";
                Console.WriteLine(message);
            }
            Console.ResetColor();
        }
        else 
        {
            await next(context);
        }
    }
}
