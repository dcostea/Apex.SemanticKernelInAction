using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

PersistentAgentsClient client = new(configuration["AzureOpenAIAgent:Endpoint"]!, new DefaultAzureCredential());
BinaryData responseFormat = BinaryData.FromString("""{ "type": "json_object" }""");
PersistentAgent persistentAgent = await client.Administration.CreateAgentAsync(
    configuration["AzureOpenAIAgent:DeploymentName"]!,
    responseFormat: responseFormat);
        
AzureAIAgent agent = new(persistentAgent, client)
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    Arguments = new()
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    },
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
            You have to break down the provided complex commands into basic moves you know.
            Respond only with the permitted moves, without any additional explanations.
            Output format: JSON array of strings, e.g. { "steps": [ "basic_move1", "basic_move2", "basic_move3", ... ] }
            """
    )),
};

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}

await client.Administration.DeleteAgentAsync(agent.Id);
