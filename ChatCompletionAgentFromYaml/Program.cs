using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

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

#pragma warning disable SKEXP0110 // ChatCompletionAgentFactory is experimental.
ChatCompletionAgentFactory factory = new();

var agent = await factory.CreateAgentFromYamlAsync("""
    type: chat_completion_agent
    name: RobotCarAgent
    description: Robot Car Agent
    instructions: |
        You are an AI assistant controlling a robot car.
        The available robot car permitted moves are {{$basic_moves}}.
        Respond only with the permitted moves, without any additional explanations.
    model:
        options:
            temperature: 0.1
    inputs:
        basic_moves:
            description: The basic moves of a robot car.
            required: true
            default: forward, backward, turn left, turn right, and stop
    tools:
      - id: MotorsPlugin.forward
        type: function
      - id: MotorsPlugin.backward
        type: function
      - id: MotorsPlugin.turn_left
        type: function
      - id: MotorsPlugin.turn_right
        type: function
      - id: MotorsPlugin.stop
        type: function
    template:
        format: semantic-kernel
    """,
    new AgentCreationOptions { Kernel = kernel });

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent!.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}
