using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

var promptTemplateConfig = new PromptTemplateConfig
{
    Name = "SemanticKernelPrompt",
    Description = "Semantic Kernel prompt template for a robot car assistant.",
    Template = """
        You are an AI assistant controlling a robot car capable of performing basic moves: {{ $input }}.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the moves, without any additional explanations.
        """,
    TemplateFormat = "semantic-kernel",
    InputVariables =
        [
            new() { Name = "input", Description = "forward, backward, turn left, turn right, and stop", IsRequired = false, Default = "" }
        ],
    ExecutionSettings = new()
    {
        ["default"] = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1000,
            Temperature = 0
        },
        ["OPENAI-gpt4o"] = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 4000,
            Temperature = 0.2f
        },
        ["AZURE-gpt-4o"] = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 20000,
            Temperature = 0.3f
        }
    },
};

var renderedPromptTemplate = await new KernelPromptTemplateFactory()
    .Create(promptTemplateConfig) // Creates the prompt template using a prompt string.
    .RenderAsync(kernel); // Renders the system prompt

var chatHistory = new ChatHistory(renderedPromptTemplate); // Add rendered system prompt to chat history

string userMessage = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";
chatHistory.AddUserMessage(userMessage);

var response = await chatCompletion.GetChatMessageContentAsync(chatHistory);
Console.WriteLine(response);
