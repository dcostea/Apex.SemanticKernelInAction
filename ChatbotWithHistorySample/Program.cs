using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
        endpoint: configuration["AzureOpenAI:Endpoint"]!,
        apiKey: configuration["AzureOpenAI:ApiKey"]!)
//.AddOpenAIChatCompletion(
//     modelId: configuration["OpenAI:ModelId"]!,
//     apiKey: configuration["OpenAI:ApiKey"]!)
.Build();

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
};

var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.
    """
);

while (true)
{
    Console.Write(" User >>> ");
    var prompt = Console.ReadLine();
    if (string.IsNullOrEmpty(prompt)) break;

    history.AddUserMessage(prompt);

    var response = await chat.GetChatMessageContentAsync(history, executionSettings);

    Console.WriteLine($"  Refusal: {response.Metadata!["Refusal"]}");



    Console.WriteLine($"  Bot >>> {response.Content}");

    history.Add(response);
}
