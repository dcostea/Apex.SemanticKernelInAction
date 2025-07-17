using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Plugins;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.Plugins.AddFromType<SearchPlugin>();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > Ask me about weather details between 1 and 10 June. (hit 'enter' key to end the session)");

var history = new ChatHistory("""
    You are an assistant who responds using ONLY the retrieved information from indexed documents.
    You can use the SearchPlugin-GetTextSearchResults tool to search for more context.
    If the information doesn't contain the answer, respond with 'I don't have enough information to answer this question.'
    """);
var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),
};

var prompt = """
    Question: {{$query}}
    """;
var promptTemplateFactory = new KernelPromptTemplateFactory();
var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(prompt));

do
{
    // Read the user question
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var query = Console.ReadLine();
    if (string.IsNullOrEmpty(query)) break;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Assistant > ");

    var kernelArguments = new KernelArguments(executionSettings)
    {
        ["query"] = query
    };
    var renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
    history.AddUserMessage(renderedPrompt);

    Console.ForegroundColor = ConsoleColor.Green;
    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    Console.WriteLine(response.Content);
    history.AddAssistantMessage(response.Content!);
}
while (true);
