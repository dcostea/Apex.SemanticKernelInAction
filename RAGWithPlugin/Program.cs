using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Data;
using Plugins;
using Filters;
using Models;
using Services;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

const string RagFilesDirectory = @"C:\Temp\RAG_Files";

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.AddInMemoryVectorStore("sktest");
builder.Services.AddInMemoryVectorStoreRecordCollection("sktest",
    options: new InMemoryVectorStoreRecordCollectionOptions<string, TextBlock>()
    {
        EmbeddingGenerator = new OpenAITextEmbeddingGenerationService(
            modelId: configuration["OpenAI:EmbeddingModelId"]!,
            apiKey: configuration["OpenAI:ApiKey"]!).AsEmbeddingGenerator(),
    });
builder.Services.AddVectorStoreTextSearch<TextBlock>();
builder.Services.AddSingleton<IDataLoader, DataLoader>();
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, AugmentingFilter>();
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.Plugins.AddFromType<SearchHookingPlugin>();

var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<TextBlock>>();
kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

var dataLoader = kernel.Services.GetRequiredService<IDataLoader>();
await dataLoader.LoadTextAsync(RagFilesDirectory);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > What would you like to know? (Type 'exit' to end the session)");
Console.WriteLine();

var history = new ChatHistory("""
    You are an assistant that responds to general questions.
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

var promptTemplateConfig = new PromptTemplateConfig()
{
    Template = prompt,
    TemplateFormat = "semantic-kernel",
    Name = "SemanticKernelPromptTemplate",
    Description = "Semantic Kernel prompt template"
};
var promptTemplateFactory = new KernelPromptTemplateFactory();
var promptTemplate = promptTemplateFactory.Create(promptTemplateConfig);

bool continueChat = true;

do
{
    // Read the user question
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var query = Console.ReadLine();

    if (string.IsNullOrEmpty(query))
    {
        continue;
    }

    if (string.Equals(query, "exit", StringComparison.OrdinalIgnoreCase))
    {
        continueChat = false;
        continue;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nAssistant > ");

    var kernelArguments = new KernelArguments(executionSettings)
    {
        ["query"] = query
    };

    var renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
    history.AddUserMessage(renderedPrompt);
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("\n===========================================");
    Console.WriteLine(renderedPrompt);
    Console.WriteLine("===========================================");

    try
    {
        Console.ForegroundColor = ConsoleColor.Green;
        string fullMessage = string.Empty;
        await foreach (var messageChunk in chat.GetStreamingChatMessageContentsAsync(history, executionSettings, kernel))
        {
            Console.Write(messageChunk.Content);
            fullMessage += messageChunk.Content;
        }
        Console.WriteLine("\n");

        // Replace the last user message (which contains the full rendered prompt) with just the original question
        history.Where(h => h.Role == AuthorRole.User).ToList().RemoveAt(0); // Remove the last user message
        history.AddUserMessage(query); // Add back just the original question
        history.AddAssistantMessage(fullMessage);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Call to LLM failed with error: {ex}");
    }
}
while (continueChat);
