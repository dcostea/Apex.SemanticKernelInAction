using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

//var textGeneration = new AzureOpenAIChatCompletionService(
//    configuration["AzureOpenAI:DeploymentName"]!,
//    configuration["AzureOpenAI:Endpoint"]!,
//    configuration["AzureOpenAI:ApiKey"]!);
var textGeneration = new OpenAIChatCompletionService(
     configuration["OpenAI:ModelId"]!,
     configuration["OpenAI:ApiKey"]!);

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    ChatSystemPrompt = """
        You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.
        """
};

var response = await textGeneration.GetChatMessageContentAsync("""
    There is a tree directly in front of the car. Avoid it and then come back to the original path.
    """,
executionSettings);

Console.WriteLine(response);
