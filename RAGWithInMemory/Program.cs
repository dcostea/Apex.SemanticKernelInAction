using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using RAGWithInMemory.Services;
using RAGWithInMemory.Models;

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
Console.WriteLine("Assistant > What would you like to know from the loaded texts? (Type 'exit' to end the session)");
Console.WriteLine();

var history = new ChatHistory("""
    You are an assistant that responds including citations to the 
    relevant information where it is referenced in the response.
    """);
var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    //FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
};

var prompt = """
    Please use this information to answer the question:
        -----------------
    {{#with (SearchPlugin-GetTextSearchResults query)}}  
        {{#each this}}  
        Name: {{Name}}
        Value: {{Value}}
        Link: {{Link}}
        -----------------
        {{/each}}
    {{/with}}
    
    Question: {{query}}
    """;

var promptTemplateConfig = new PromptTemplateConfig()
{
    Template = prompt,
    TemplateFormat = "handlebars",
    Name = "HandlebarsPrompt",
    Description = "Handlebars prompt template"
};
var promptTemplateFactory = new HandlebarsPromptTemplateFactory();
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
