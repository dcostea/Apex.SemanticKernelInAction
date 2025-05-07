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

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
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

var cancellationToken = new CancellationTokenSource().Token;

var dataLoader = kernel.Services.GetRequiredService<IDataLoader>();
await dataLoader.LoadPdf("sample.pdf", 2, 1000, cancellationToken);
Console.WriteLine("PDF loading complete\n");

var vectorStoreCollection = kernel.Services.GetRequiredService<IVectorStoreRecordCollection<string, TextSnippet>>();
var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<TextSnippet>>();
kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Assistant > What would you like to know from the loaded PDF?");

// Read the user question.
Console.ForegroundColor = ConsoleColor.White;
Console.Write("User > ");
var question = Console.ReadLine();

var response = kernel.InvokePromptStreamingAsync(
    promptTemplate: """
       Please use this information to answer the question:
       {{#with (SearchPlugin-GetTextSearchResults question)}}  
         {{#each this}}  
         Name: {{Name}}
         Value: {{Value}}
         Link: {{Link}}
         -----------------
         {{/each}}
       {{/with}}

       Include citations to the relevant information where it is referenced in the response.
                   
       Question: {{question}}
       """,
    arguments: new KernelArguments()
    {
        { "question", question },
    },
    templateFormat: "handlebars",
    promptTemplateFactory: new HandlebarsPromptTemplateFactory(),
    cancellationToken: cancellationToken);

Console.ForegroundColor = ConsoleColor.Green;
Console.Write("\nAssistant > ");

try
{
    await foreach (var message in response.ConfigureAwait(false))
    {
        Console.Write(message);
    }
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Call to LLM failed with error: {ex}");
}
