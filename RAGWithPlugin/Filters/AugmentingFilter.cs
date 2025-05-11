using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;

namespace Filters;

public sealed class AugmentingFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        if (context.Kernel.Plugins.TryGetFunction("SearchPlugin", "GetTextSearchResults", out var getSearchFunction))
        {
            var kernelArguments = new KernelArguments()
            {
                ["query"] = context.ChatHistory.Where(c => c.Role == AuthorRole.User).Last().Content
            };
            var response = await context.Kernel.InvokeAsync<List<TextSearchResult>>(getSearchFunction, kernelArguments);
            var augmentation = string.Join('\n', response!
                .Select(s => $"""
                    Name: {s.Name}
                    Link: {s.Link}
                    Value: {s.Value}
                    """));
            var augmentedQuery = $"""
                {kernelArguments["query"]}
                
                Context: 
                {augmentation}
                """;
            context.ChatHistory.Where(c => c.Role == AuthorRole.User).Last().Content = augmentedQuery;
        }

        await next(context);

        context.Terminate = false;
    }
}
