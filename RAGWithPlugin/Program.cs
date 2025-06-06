using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Data;
using Plugins;
using Filters;
using Models;
using Services;
using Microsoft.Extensions.AI;
using OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

const string RagFilesDirectory = @"Data";

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddSingleton<IEmbeddingGenerator>(sp => 
    new OpenAIClient(configuration["OpenAI:ApiKey"]!)
        .GetEmbeddingClient(configuration["OpenAI:EmbeddingModelId"]!)
        .AsIEmbeddingGenerator());
builder.Services.AddInMemoryVectorStoreRecordCollection<string, TextBlock>("sktest");
builder.Services.AddVectorStoreTextSearch<TextBlock>();
builder.Services.AddSingleton<ITextLoader, TextLoader>();
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, AugmentingFilter>();
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.Plugins.AddFromType<SearchPlugin>();

var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<TextBlock>>();
kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

var dataLoader = kernel.Services.GetRequiredService<ITextLoader>();
await dataLoader.LoadAsync(RagFilesDirectory);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > What would you like to know? (hit 'enter' key to end the session)");
Console.WriteLine();

var history = new ChatHistory("""
    You are an assistant that responds using ONLY the retrieved information.
    If the information doesn't contain the answer, respond with 'I don't have enough information to answer this question.'
    """);
var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
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
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("===========================================");
    Console.WriteLine(renderedPrompt);
    Console.WriteLine("===========================================");

    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    Console.WriteLine(response.Content);
    history.AddAssistantMessage(response.Content!);
}
while (true);
