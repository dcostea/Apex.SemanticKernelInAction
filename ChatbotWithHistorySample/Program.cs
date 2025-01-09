using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernel = Kernel.CreateBuilder()
//.AddAzureOpenAIChatCompletion(
//deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//endpoint: configuration["AzureOpenAI:Endpoint"]!,
//apiKey: configuration["AzureOpenAI:ApiKey"]!)
.AddOpenAIChatCompletion(
     modelId: configuration["OpenAI:ModelId"]!,
     apiKey: configuration["OpenAI:ApiKey"]!)
.Build();

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
};

var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    Your task is to break down complex commands into a sequence of these basic moves.
    Provide only the sequence of the basic movements, without any additional explanations.
    """
);

while (true)
{
    Console.Write(" User >>> ");
    var prompt = Console.ReadLine();
    if (string.IsNullOrEmpty(prompt)) break;

    history.AddUserMessage(prompt);

    var response = await chat.GetChatMessageContentAsync(history, executionSettings);

    Console.WriteLine($"  Bot >>> {response.Content}");

    history.Add(response);
}
