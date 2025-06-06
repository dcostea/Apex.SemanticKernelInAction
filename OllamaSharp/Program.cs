using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

const string ModelUri = "http://localhost:11434";
const string Model = "llama3.1:latest";
var ollamaClient = new OllamaApiClient(new Uri(ModelUri), defaultModel: Model);

var request = new GenerateRequest
{
    //System = """
    //    You are an AI assistant controlling a robot car.
    //    """,
    Prompt = """
        You are an AI assistant controlling a robot car.
        
        Your task is to break down complex commands into a sequence for basic moves such as forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.
            
        Complex command:
        "There is a tree directly in front of the car. Avoid it and then come back to the original path."
        """,
    Stream = true,
    Options = new RequestOptions
    {
        Temperature = 0.1F,
        TopP = 0.95F,
        NumPredict = 2000,
    }
};


/// GenerateAsync /////////////////////////////////////
Console.WriteLine($"RESPONSE: ");
await foreach (var partialResponse in ollamaClient.GenerateAsync(request))
{
    Console.Write(partialResponse?.Response);
}
Console.WriteLine();


/// ChatAsync /////////////////////////////////////
Console.WriteLine();

var chatRequest = new ChatRequest
{
    Messages =
    [
        new Message
        {
            Role = ChatRole.System,
            Content = """
                You are an AI assistant controlling a robot car.
                """
        },
        new Message
        {
            Role = ChatRole.User,
            Content = """
                Your task is to break down complex commands into a sequence for basic moves such as forward, backward, turn left, turn right, and stop.
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                        
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """
        }
    ],
    Stream = true,
    Options = new RequestOptions
    {
        Temperature = 0.1F,
        TopP = 0.95F,
        NumPredict = 2000,
    }
};

Console.WriteLine($"RESPONSE: ");
await foreach (var partialResponse in ollamaClient.ChatAsync(chatRequest))
{
    Console.Write(partialResponse?.Message.Content);
}
Console.WriteLine();


/// GetChatMessageContentAsync /////////////////////////////////////

var executionSettings = new OpenAIPromptExecutionSettings
{
    //FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    Temperature = 0.1F,
    TopP = 0.95F,
    MaxTokens = 2000,
};

Console.WriteLine();
var prompt = """
    You are an AI assistant controlling a robot car.

    Your task is to break down complex commands into a sequence for basic moves such as forward, backward, turn left, turn right, and stop.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the permitted moves, without any additional explanations.
            
    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    """;
var chat = ollamaClient.AsChatCompletionService();

var response = await chat.GetChatMessageContentAsync(prompt, executionSettings);
Console.WriteLine($"RESPONSE: {response.Content}");

Console.WriteLine($"RESPONSE: ");
await foreach (var partialResponse in chat.GetStreamingChatMessageContentsAsync(prompt, executionSettings))
{
    Console.Write(partialResponse.Content);
}
Console.WriteLine();

//Console.WriteLine();
//var chatOption = new Microsoft.Extensions.AI.ChatOptions
//{
//    Temperature = 0.1F,
//    TopP = 0.95F,
//    Seed = 42,
//};
//Console.WriteLine($"RESPONSE: ");
//await foreach (var partialResponse in ollamaClient.GetStreamingResponseAsync("", chatOption))
//{
//    Console.Write(partialResponse!.Text);
//}
