﻿using Microsoft.SemanticKernel;

namespace Filters;

public sealed class PromptAuditFilter : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(PromptAuditFilter)} prompt rendering {context.Function.Name}");
        Console.ResetColor();

        await next(context);

        var rendered = context.RenderedPrompt;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {nameof(PromptAuditFilter)} prompt rendered {context.Function.Name}");
        Console.WriteLine($"  Rendered prompt: {rendered}");
        Console.ResetColor();
    }
}