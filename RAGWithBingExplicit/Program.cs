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
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var textSearch = new BingTextSearch(configuration["BingSearchKey"]!);
var searchPlugin = textSearch.CreateWithSearch("SearchPlugin");
kernel.Plugins.Add(searchPlugin);

var prompt = """
    Please use this information to answer the question:
    -----------------
    {{SearchPlugin.Search $query}}
    
    Question: {{$query}}
    """;

Console.WriteLine("Assistant > What would you like to know? (Hit 'enter' key to end the session)");
  
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1
};

Console.ForegroundColor = ConsoleColor.White;
Console.Write("User > ");
var query = Console.ReadLine();

var kernelArguments = new KernelArguments(executionSettings)
{
    ["query"] = query,
    ["count"] = 5
};
var response = await kernel.InvokePromptAsync(prompt, kernelArguments);

Console.ForegroundColor = ConsoleColor.Green;
Console.Write($"Assistant > {response}");
Console.ResetColor();
