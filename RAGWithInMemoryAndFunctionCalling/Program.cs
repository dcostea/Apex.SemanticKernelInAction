using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using RAGWithInMemoryAndFunctionCalling.Models;
using RAGWithInMemoryAndFunctionCalling.Services;

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
var kernel = builder.Build();

var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<TextBlock>>();
kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

var dataLoader = kernel.Services.GetRequiredService<IDataLoader>();
await dataLoader.LoadTextAsync(RagFilesDirectory);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > What would you like to know from the loaded files? (Type 'exit' to end the session)");
Console.WriteLine();

var history = new ChatHistory("""
    You are an assistant that responds to question using available information.
    You can use the SearchPlugin GetTextSearchResults function from your tools to search for information, if needed.
    But always include citations to the relevant information where it is referenced in the response.
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
    Name = "SemanticKernelPrompt",
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
        ["query"] = query,
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

        history.AddAssistantMessage(fullMessage);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Call to LLM failed with error: {ex}");
    }
}
while (continueChat);
