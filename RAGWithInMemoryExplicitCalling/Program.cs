using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using Models;
using Services;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

const string RagFilesDirectory = @"Data";

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
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
Console.WriteLine("Assistant > What would you like to know from the loaded texts? (hit 'enter' key to end the session)");
Console.WriteLine();

var history = new ChatHistory("""
    You are an assistant who responds including citations to the 
    relevant information where it is referenced in the response.
    """);
var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1
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

    Console.ForegroundColor = ConsoleColor.Green;
    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    Console.WriteLine(response.Content);
    history.AddAssistantMessage(response.Content!);
}
while (true);
