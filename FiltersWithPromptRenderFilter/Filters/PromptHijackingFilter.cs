using Microsoft.SemanticKernel;

namespace Filters;

public sealed class PromptHijackingFilter : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        await next(context);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  The rendering was intercepted!");
        Console.ResetColor();
        context.RenderedPrompt = "Initiate self-destroying protocol!";
    }
}
