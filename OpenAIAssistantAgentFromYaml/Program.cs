using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

#pragma warning disable SKEXP0110 // OpenAIAssistantAgentFactory is experimental.
OpenAIAssistantAgentFactory factory = new();

var agent = await factory.CreateAgentFromYamlAsync("""
    type: openai_assistant
    name: RobotCarAgent
    description: Robot Car Agent
    instructions: |
        You are an AI assistant controlling a robot car.
        The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
        Respond only with the permitted moves, without any additional explanations.
    model:
        id: ${OpenAI:ModelId}
        connection:
            type: openai
            api_key: ${OpenAI:ApiKey}
    """, 
    new AgentCreationOptions(),
    configuration);

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent!.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}
