﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using Models;
using Services;
using OpenAI;
using System.Text;

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
builder.Services.AddSingleton<IPdfLoader, PdfLoader>();
var kernel = builder.Build();

var pdfDataLoader = kernel.Services.GetRequiredService<IPdfLoader>();
await pdfDataLoader.LoadAsync(RagFilesDirectory);
var textDataLoader = kernel.Services.GetRequiredService<ITextLoader>();
await textDataLoader.LoadAsync(RagFilesDirectory);

var vectorStoreTextSearch = kernel.Services.GetRequiredService<VectorStoreTextSearch<TextBlock>>();
kernel.Plugins.Add(vectorStoreTextSearch.CreateWithGetTextSearchResults("SearchPlugin"));

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > Ask me anything from the loaded PDF. (hit 'enter' key to end the chat session)");
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

    try
    {
        Console.ForegroundColor = ConsoleColor.Green;
        StringBuilder messageBuilder = new();
        await foreach (var messagePart in chat.GetStreamingChatMessageContentsAsync(history, executionSettings, kernel))
        {
            Console.Write(messagePart.Content);
            messageBuilder.Append(messagePart.Content);
        }
        Console.WriteLine("\n");

        // Replace the last user message (which contains the full rendered prompt) with just the original question
        history.Where(h => h.Role == AuthorRole.User).ToList().RemoveAt(0); // Remove the last user message
        history.AddUserMessage(query); // Add back just the original question
        history.AddAssistantMessage(messageBuilder.ToString());
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Call to LLM failed with error: {ex}");
    }
}
while (true);
