using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var textSearch = new BingTextSearch(configuration["BingSearchKey"]!);
var searchPlugin = textSearch.CreateWithGetSearchResults("SearchPlugin");
kernel.Plugins.Add(searchPlugin);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > What would you like to know? (hit 'enter' key to end the session)");

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),
    ChatSystemPrompt = """
        You are an assistant connected to a web search engine.
        """,
};

Console.ForegroundColor = ConsoleColor.White;
Console.Write("User > ");
var query = Console.ReadLine();

var kernelArguments = new KernelArguments(executionSettings)
{
    ["query"] = query
};

var prompt = $"""
    {query}
    
    (always search the web for details)
    """;
    
var response = await kernel.InvokePromptAsync(prompt, kernelArguments);

Console.ForegroundColor = ConsoleColor.Green;
Console.Write($"Assistant > {response}");
Console.ResetColor();
