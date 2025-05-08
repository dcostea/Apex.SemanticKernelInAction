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
using RAGWithVolatile.Models;
using RAGWithVolatile.Services;
using Microsoft.SemanticKernel.ChatCompletion;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.AddInMemoryVectorStore("sktest");
builder.Services.AddInMemoryVectorStoreRecordCollection("sktest",
    options: new InMemoryVectorStoreRecordCollectionOptions<string, TextSnippet>()
    {
        EmbeddingGenerator = new OpenAITextEmbeddingGenerationService(
            modelId: configuration["OpenAI:EmbeddingModelId"]!,
            apiKey: configuration["OpenAI:ApiKey"]!).AsEmbeddingGenerator(),
    });
builder.Services.AddVectorStoreTextSearch<TextSnippet>();
builder.Services.AddSingleton<IDataLoader, DataLoader>();
var kernel = builder.Build();

var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<TextSnippet>>();
kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

var cancellationToken = new CancellationTokenSource().Token;

var dataLoader = kernel.Services.GetRequiredService<IDataLoader>();
await dataLoader.IndexPdfs(cancellationToken);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > What would you like to know from the loaded PDF? (Type 'exit' to end the session)");
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
    {{#with (SearchPlugin-GetTextSearchResults question)}}  
        {{#each this}}  
        Name: {{Name}}
        Value: {{Value}}
        Link: {{Link}}
        -----------------
        {{/each}}
    {{/with}}
    
    Question: {{question}}
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
    var question = Console.ReadLine();

    if (string.IsNullOrEmpty(question))
    {
        continue;
    }

    if (string.Equals(question, "exit", StringComparison.OrdinalIgnoreCase))
    {
        continueChat = false;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Assistant > Chat session ended. Goodbye!");
        continue;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nAssistant > ");

    var kernelArguments = new KernelArguments(executionSettings)
    {
        { "question", question }
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
        await foreach (var messageChunk in chat.GetStreamingChatMessageContentsAsync(history, executionSettings, kernel, cancellationToken))
        {
            Console.Write(messageChunk.Content);
            fullMessage += messageChunk.Content;
        }
        Console.WriteLine("\n");

        // Replace the last user message (which contains the full rendered prompt) with just the original question
        history.RemoveAt(history.Count - 1); // Remove the last user message
        history.AddUserMessage(question); // Add back just the original question
        history.AddAssistantMessage(fullMessage);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Call to LLM failed with error: {ex}");
    }
}
while (continueChat);
