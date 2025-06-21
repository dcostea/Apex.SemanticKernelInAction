using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

PersistentAgentsClient client = new(configuration["AzureOpenAIAgent:Endpoint"]!, new DefaultAzureCredential());
PersistentAgent definition = await client.Administration.CreateAgentAsync(
    configuration["AzureOpenAIAgent:DeploymentName"]!,
    tools: [new CodeInterpreterToolDefinition()]);
#pragma warning disable SKEXP0110 // AzureAIAgent is experimental
AzureAIAgent agent = new(definition, client)
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    Arguments = new()
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    },
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            ## PERSONA
            You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
            """
    ))
};

var query = """
    ## ACTION
    Navigate the car from point A to point B using the diagonal path.
    Reasoning steps:
     - Calculate the distance to travel along the diagonal path
     - Calculate the angle and the direction to turn
     - Round up the distance and the angle to integers when necessary.
     - Then turn the car and proceed.
                
    ## CONTEXT
    You are facing a field of 10 meters wide and 20 meters long.
    You are in one of the corners of the field (point A) looking along the edge, and you want to get to the opposing corner (point B).
    You are allowed to use the code interpreter to perform calculations and reasoning.
    
    ## OUTPUT TEMPLATE
    Respond only with the basic moves (with angles and distances), without any additional explanations.
    """;

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}

await client.Administration.DeleteAgentAsync(agent.Id);
