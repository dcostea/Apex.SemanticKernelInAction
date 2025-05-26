using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

const string SourceFilePath = @"Data\weather.txt";

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var context = File.ReadAllText(SourceFilePath);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > Ask me about weather details between 1 and 10 of June. (Hit 'enter' key to end the session)");

var history = new ChatHistory();
var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
};

//var s = new ChatHistorySummarizationReducer(chat, 5, 2);
////ChatHistoryTruncationReducer(5, 2);
////ChatHistoryMaxTokensReducer
////ChatHistorySummarizationReducer
var prompt = """
    Question: {{$query}}
    Context: {{$context}}
    """;
var promptTemplateFactory = new KernelPromptTemplateFactory();
var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(prompt));

do
{
    // Read the user question
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var query = Console.ReadLine();

    if (string.IsNullOrEmpty(query)) break;

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Assistant > ");

    var kernelArguments = new KernelArguments(executionSettings)
    {
        ["query"] = query,
        ["context"] = context
    };
    var renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
    history.AddUserMessage(renderedPrompt);


    Console.ForegroundColor = ConsoleColor.Green;
    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    Console.WriteLine(response.Content);
    history.AddAssistantMessage(response.Content!);

    //var reduced = await s.ReduceAsync(history);
    //if (reduced is not null)
    //{
    //    var h = new ChatHistory(reduced!);
    //    history = h;
    //}
}
while (true);
