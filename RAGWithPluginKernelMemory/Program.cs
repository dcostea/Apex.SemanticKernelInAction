using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    configuration["AzureOpenAI:DeploymentName"]!,
    configuration["AzureOpenAI:Endpoint"]!,
    configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    configuration["OpenAI:ModelId"]!,
//    configuration["OpenAI:ApiKey"]!);
var kernel = builder.Build();

// === PREPARE MEMORY PLUGIN ===
// Load the Kernel Memory plugin into Semantic Kernel.
// We're using a local instance here, so remember to start the service locally first,
// otherwise change the URL pointing to your KM endpoint.

var kernelMemory = new KernelMemoryBuilder()
    .WithAzureOpenAITextGeneration(new AzureOpenAIConfig
    {
        APIKey = configuration["AzureOpenAI:ApiKey"]!,
        Deployment = configuration["AzureOpenAI:DeploymentName"]!,
        Endpoint = configuration["AzureOpenAI:Endpoint"]!
    })
    .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig
    {
        APIKey = configuration["AzureOpenAI:ApiKey"]!,
        Deployment = configuration["AzureOpenAI:EmbeddingDeploymentName"]!,
        Endpoint = configuration["AzureOpenAI:Endpoint"]!
    })
    //.WithOpenAIDefaults(configuration["OpenAI:ApiKey"]!)
    .Build<MemoryServerless>();

var memoryPlugin = kernel.ImportPluginFromObject(new MemoryPlugin(kernelMemory, waitForIngestionToComplete: true), "memory");

// ==================================
// === LOAD DOCUMENTS INTO MEMORY ===
// ==================================

// Load some data in memory, in this case use a PDF file, though
// you can also load web pages, Word docs, raw text, etc.
// We load data in the default index (used when an index name is not specified)
// and some different data in the "private" index.

// You can use either the plugin or the connector, the result is the same
//await memoryConnector.ImportDocumentAsync(filePath: DocFilename, documentId: "WEATHER001");
var context = new KernelArguments
{
    [MemoryPlugin.FilePathParam] = @"Data/Weather2.pdf",
    [MemoryPlugin.DocumentIdParam] = "WEATHER001"
};
await memoryPlugin["SaveFile"].InvokeAsync(kernel, context);

context = new KernelArguments
{
    ["index"] = "private",
    ["input"] = "From 1st to 5th of June everyday is zero celsius degrees and the wind is steady.",
    [MemoryPlugin.DocumentIdParam] = "PRIVATE01"
};
await memoryPlugin["Save"].InvokeAsync(kernel, context);

// ==============================================
// === RUN SEMANTIC FUNCTION ON DEFAULT INDEX ===
// ==============================================

// Run some example questions, showing how the answer is grounded on the document uploaded.
// Only the first question can be answered, because the document uploaded doesn't contain any
// information about Question2 and Question3.

const string query = "what is the temperature on 6st of June?";
Console.WriteLine($"QUERY: {query}");

var prompt = """
    Please use this information to answer the question:
    -----------------    
    {{memory.ask $input}}
    -----------------

    Question: {{$input}}
    """;

//{{memory.ask $input index='private'}}
var executionSettings = new OpenAIPromptExecutionSettings
{
    ChatSystemPrompt = """Answer or say "I don't know".""",
    MaxTokens = 1000,
    Temperature = 0.1F
};
var augmentedFunction = kernel.CreateFunctionFromPrompt(prompt, executionSettings);

var response = await augmentedFunction.InvokeAsync(kernel, query);

Console.WriteLine($"RESPONSE: {response}");
