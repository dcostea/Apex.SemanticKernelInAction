using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.ChatCompletion;
using RAGWithVolatile;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData.ConnectorSupport;
using Xunit;
using OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.AddOpenAITextEmbeddingGeneration(
//    modelId: configuration["OpenAI:EmbeddingModelId"]!,
//    apiKey: configuration["OpenAI:ApiKey"]!);

builder.Services.AddSingleton<IEmbeddingGenerator>(sp =>
    new OpenAIClient(configuration["OpenAI:ApiKey"]!)
    .GetEmbeddingClient(configuration["OpenAI:EmbeddingModelId"]!)
    .AsIEmbeddingGenerator());

//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
builder.AddInMemoryVectorStore("sktest");
builder.Services.AddInMemoryVectorStoreRecordCollection("sktest", 
    options: new InMemoryVectorStoreRecordCollectionOptions<string, TextSnippet>() 
    {
        EmbeddingGenerator = new OpenAITextEmbeddingGenerationService(
            modelId: configuration["OpenAI:EmbeddingModelId"]!,
            apiKey: configuration["OpenAI:ApiKey"]!).AsEmbeddingGenerator(),
    });
//builder.Services.AddVectorStoreTextSearch<TextSnippet>(
//    new TextSearchStringMapper((result) => (result as TextSnippet)!.Text!),
//    new TextSearchResultMapper((result) =>
//    {
//        // Create a mapping from the Vector Store data type to the data type returned by the Text Search.
//        // This text search will ultimately be used in a plugin and this TextSearchResult will be returned to the prompt template
//        // when the plugin is invoked from the prompt template.
//        var castResult = result as TextSnippet;
//        return new TextSearchResult(value: castResult!.Text!) 
//        {
//            Name = castResult.ReferenceDescription, 
//            Link = castResult.ReferenceLink 
//        };
//    }));
builder.Services.AddVectorStoreTextSearch<TextSnippet>();

builder.Services.AddSingleton<IDataLoader, DataLoader>();
var kernel = builder.Build();

var cancellationToken = new CancellationTokenSource().Token;

/////////////////////////////////////////////////////////////////////////////////////////
// the next code is for asserting that the embedding generator is set up correctly
//using var embeddingGenerator = new OpenAITextEmbeddingGenerationService(
//            modelId: configuration["OpenAI:EmbeddingModelId"]!,
//            apiKey: configuration["OpenAI:ApiKey"]!).AsEmbeddingGenerator();
//#pragma warning disable MEVD9001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//var x = new VectorStoreRecordModelBuilder(new VectorStoreRecordModelBuildingOptions
//{
//    SupportsMultipleKeys = true, // Set to true or false based on your requirements
//    SupportsMultipleVectors = true, // Set to true or false based on your requirements
//    RequiresAtLeastOneVector = true, // Set to true or false based on your requirements
//    SupportedKeyPropertyTypes = new HashSet<Type> { typeof(string), typeof(int) }, // Add appropriate types
//    SupportedDataPropertyTypes = new HashSet<Type> { typeof(string), typeof(int) }, // Add appropriate types
//    SupportedEnumerableDataPropertyElementTypes = new HashSet<Type> { typeof(string), typeof(int) }, // Add appropriate types
//    SupportedVectorPropertyTypes = new HashSet<Type> { typeof(ReadOnlyMemory<float>) } // Add appropriate types
//});
//var model = x.Build(typeof(TextSnippet), vectorStoreRecordDefinition: null, embeddingGenerator);
//Assert.Same(embeddingGenerator, model.VectorProperty.EmbeddingGenerator);
/////////////////////////////////////////////////////////////////////////////////////////




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

var kernelArguments = new KernelArguments
{
    ["query"] = question,
};
var has = kernel.Plugins.TryGetFunction("SearchPlugin", "GetTextSearchResults", out var function);
var response0 = await kernel.InvokeAsync(function!, kernelArguments);

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
