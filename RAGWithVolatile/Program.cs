using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading;
using RAGWithVolatile;
using System.Collections;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);

builder.AddOpenAITextEmbeddingGeneration(
    modelId: configuration["OpenAI:EmbeddingModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);

builder.AddInMemoryVectorStore();
builder.Services.AddSingleton<IDataLoader, DataLoader>();
builder.Services.AddSingleton<VectorStoreTextSearch<TextSnippet>>();
builder.Services.AddInMemoryVectorStoreRecordCollection<string, TextSnippet>("sktest");

var kernel = builder.Build();


var cancellationToken = new CancellationTokenSource().Token;

var dataLoader = kernel.Services.GetRequiredService<IDataLoader>();
await dataLoader.LoadPdf("sample.pdf", 2, 1000, cancellationToken);
Console.WriteLine("PDF loading complete\n");

var vectorStore = new InMemoryVectorStore();
var collection = vectorStore.GetCollection<string, TextParagraph>("sktest");

// Create the collection if it doesn't exist yet.
await collection.CreateCollectionIfNotExistsAsync();

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

// Stream the LLM response to the console with error handling.
Console.ForegroundColor = ConsoleColor.Green;
Console.Write("\nAssistant > ");

try
{
    await foreach (var message in response)
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

// Upsert a record.
//string descriptionText1 = "Semantic Kernel is an orchestrator.";
//string descriptionText2 = "Semantic Kernel is an SDK.";
//string descriptionText3 = "Langchain is an orchestrator.";

//string id1 = Guid.CreateVersion7().ToString();
//string id2 = Guid.CreateVersion7().ToString();
//string id3 = Guid.CreateVersion7().ToString();

////await collection.UpsertAsync(new TextParagraph
////{
////    Key = id1,
////    Text = descriptionText1,
////    ParagraphId = "Ref1",
////    DocumentUri = "url1",
////    TextEmbedding = await GenerateEmbeddingAsync(kernel, descriptionText1),
////});

////await collection.UpsertAsync(new TextParagraph
////{
////    Key = id2,
////    Text = descriptionText2,
////    ParagraphId = "Ref2",
////    DocumentUri = "url2",
////    TextEmbedding = await GenerateEmbeddingAsync(kernel, descriptionText2),
////});

////await collection.UpsertAsync(new TextParagraph
////{
////    Key = id3,
////    Text = descriptionText3,
////    ParagraphId = "Ref3",
////    DocumentUri = "url3",
////    TextEmbedding = await GenerateEmbeddingAsync(kernel, descriptionText3),
////});

// Retrieve the upserted record.
//var retrieved = await collection.GetAsync(id2);

//if (retrieved is not null)
//{
//    Console.WriteLine(retrieved.Text);
//}
