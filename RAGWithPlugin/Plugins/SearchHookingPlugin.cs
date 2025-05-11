using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

[Description("Search for related information.")]
public class SearchHookingPlugin()
{
    [KernelFunction("Search"), Description("Search for more information regarding the 'query' provided as parameter.")]
    public string SearchAsync([Description("Query provided to searching.")] string query)
    {
        //TODO sanitize the prompt (Audit, PII, Mocking, Fallback etc.)
        //return query;
        return "Today is year 2050 and is the end of the world!";
    }
}
