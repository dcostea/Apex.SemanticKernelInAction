﻿using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

//var textGeneration = new AzureOpenAIChatCompletionService(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
var chatCompletion = new OpenAIChatCompletionService(
     modelId: configuration["OpenAI:ModelId"]!,
     apiKey: configuration["OpenAI:ApiKey"]!);

var prompt = """
    <message role="system">
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    Your task is to break down complex commands into a sequence of these basic moves.
    Provide only the sequence of the basic movements, without any additional explanations.
    </message>

    ## Complex command:
    <message role="user">
    "There is a tree directly in front of the car. Avoid it and then resume the initial direction."
    </message>
    """;

//Get a single text generation result for the prompt and settings.
//Single text content generated by the remote model.</returns>
var response1 = await chatCompletion.GetTextContentAsync(prompt);

//Get completion results for the prompt and settings.
//List of different completions results generated by the remote model.
var response2 = await chatCompletion.GetTextContentsAsync(prompt);

//Get a single chat message content for the prompt and settings.
//Single chat message content generated by the remote model.
var response3 = await chatCompletion.GetChatMessageContentAsync(prompt);

//Get chat multiple chat message content choices for the prompt and settings.
//This should be used when the settings request for more than one choice.
//List of different chat message content choices generated by the remote model.
var response4 = await chatCompletion.GetChatMessageContentsAsync(prompt);

//Console.WriteLine(response);

//Get streaming results for the prompt using the specified execution settings.
//Each modality may support for different types of streaming contents.
//Usage of this method with value types may be more efficient if the connector supports it.
//Streaming list of different completion streaming string updates generated by the remote model.
await foreach (var streamingResponse in chatCompletion.GetStreamingTextContentsAsync(prompt))
{
    Console.WriteLine(streamingResponse);
}

//Get streaming chat message contents for the chat history provided using the specified settings.
//Streaming list of different completion streaming string updates generated by the remote model.
await foreach (var streamingResponse in chatCompletion.GetStreamingChatMessageContentsAsync(prompt))
{
    Console.WriteLine(streamingResponse);
}
