﻿using Json.Schema;
using OllamaSharp;
using OllamaSharp.Models;

const string ModelUri = "http://localhost:11434";
const string Model = "mistral-small3.1:latest";
var ollamaClient = new OllamaApiClient(new Uri(ModelUri), defaultModel: Model);

var request = new GenerateRequest
{
    System = """
        You are an AI assistant controlling a robot car.
        Your task is to break down complex commands into a sequence for basic moves such as forward, backward, turn left, turn right, and stop.
        Respond only with the permitted moves, without any additional explanations.
        Output format: JSON array of strings, e.g. { "steps": [ "basic_move1", "basic_move2", "basic_move3", ... ] }
        """,
    Prompt = """
        Complex command:
        "There is a tree directly in front of the car. Avoid it and then come back to the original path."
        """,
    Format = new JsonSchemaBuilder()
        .Type(SchemaValueType.Object)
        .Properties(("steps", new JsonSchemaBuilder().Type(SchemaValueType.Array)
        .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))))
        .Required("steps")
        .AdditionalProperties(false)
        .Build(),
    Stream = true,
    Options = new RequestOptions
    {
        Temperature = 0.1F,
        TopP = 0.95F,
        NumPredict = 2000,
    }
};

Console.WriteLine("=== Ollama Client with Response Format ===");

Console.WriteLine($"RESPONSE: ");
await foreach (var chunk in ollamaClient.GenerateAsync(request))
{
    Console.Write(chunk?.Response);
}
Console.WriteLine();
