﻿using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

#pragma warning disable SKEXP0110 // OpenAIAssistantAgentFactory is experimental.
OpenAIAssistantAgentFactory factory = new();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

var promptTemplateFactory = new KernelPromptTemplateFactory();

var agent = await factory.CreateAgentFromYamlAsync("""
    type: openai_assistant
    name: RobotCarAgent
    description: Robot Car Agent
    instructions: |
        You are an AI assistant controlling a robot car.
        The available robot car permitted moves are {{$basic_moves}}.
        Respond only with the permitted moves, without any additional explanations.
    model:
        id: ${OpenAI:ModelId}
        connection:
            type: openai
            api_key: ${OpenAI:ApiKey}
    inputs:
        basic_moves:
            description: The basic moves of a robot car.
            required: true
            default: forward, backward, turn left, turn right, and stop
    template:
        format: semantic-kernel
    """, 
    new AgentCreationOptions() { Kernel = kernel, PromptTemplateFactory = promptTemplateFactory },
    configuration);

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent!.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}
