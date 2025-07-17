using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

const string SourceFilePath = @"Data";

var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    configuration["OpenAI:ModelId"]!,
    configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

StringBuilder contextBuilder = new();
string[] txtFiles = Directory.GetFiles(SourceFilePath, "*.txt");

foreach (var txtFile in txtFiles)
{
    var textFileName = Path.GetFileName(txtFile);
    Console.WriteLine($"Loading {textFileName}...");
    var text = await File.ReadAllTextAsync(txtFile);
    contextBuilder.Append(text);
    Console.WriteLine($"Text {textFileName} loading complete");
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > Ask me about weather details between 1 and 10 June. (hit 'enter' key to end the session)");

var history = new ChatHistory();
var chat = kernel.GetRequiredService<IChatCompletionService>();
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
};

var prompt = """
    Question: {{$query}}
    Context: {{$context}}
    """;
var promptTemplateFactory = new KernelPromptTemplateFactory();
var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(prompt));

var kernelArguments = new KernelArguments(executionSettings)
{
    ["context"] = contextBuilder.ToString()
};

do
{
    // Read the user question
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var query = Console.ReadLine();
    if (string.IsNullOrEmpty(query)) break;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Assistant > ");

    kernelArguments["query"] = query;

    var renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
    history.AddUserMessage(renderedPrompt);

    Console.ForegroundColor = ConsoleColor.Green;
    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);
    Console.WriteLine(response.Content);
    history.AddAssistantMessage(response.Content!);
}
while (true);
