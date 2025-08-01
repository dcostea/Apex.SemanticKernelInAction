﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Assistants;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var assistantClient = new AssistantClient(configuration["OpenAI:ApiKey"]!);
var assistant = await assistantClient.CreateAssistantAsync(configuration["OpenAI:ModelId"]);

OpenAIAssistantAgent agent = new(assistant, assistantClient)
{
    Name = "RobotCarAgent",
    Instructions = """
        You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the permitted moves, without any additional explanations.
        """,
    Description = "A robot car that can perform basic moves",
    LoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace))
};

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}
