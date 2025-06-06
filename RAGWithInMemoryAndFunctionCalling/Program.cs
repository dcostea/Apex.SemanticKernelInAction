using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using Models;
using Services;
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
var kernel = builder.Build();

var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<TextBlock>>();
kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

var dataLoader = kernel.Services.GetRequiredService<ITextLoader>();
await dataLoader.LoadAsync(RagFilesDirectory);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > What would you like to know from the loaded files? (hit 'enter' key to end the session)");
Console.WriteLine();

var history = new ChatHistory("""
    You are an assistant who responds using ONLY the retrieved information from indexed documents.
    You can use the SearchPlugin-GetTextSearchResults tool to search into indexed documents.
    If the information doesn't contain the answer, respond with 'I don't have enough information to answer this question.'
    Always include citations to the relevant information where it is referenced in the response.
    """);
var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    FunctionChoiceBehavior = FunctionChoiceBehavior.None()
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
        ["query"] = query,
    };

    var renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
    history.AddUserMessage(renderedPrompt);
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("===========================================");
    Console.WriteLine(renderedPrompt);
    Console.WriteLine("===========================================");

    Console.ForegroundColor = ConsoleColor.Green;

    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    Console.WriteLine(response.Content);
    history.AddAssistantMessage(response.Content!);
}
while (true);
