using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Assistants;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

#pragma warning disable OPENAI001 // AssistantClient is experimental.
var assistantClient = new AssistantClient(configuration["OpenAI:ApiKey"]!);
var assistant = await assistantClient.CreateAssistantAsync(configuration["OpenAI:ModelId"]!, enableCodeInterpreter: true);

var motorsPlugin = KernelPluginFactory.CreateFromType<MotorsPlugin>();

OpenAIAssistantAgent agent = new(assistant, assistantClient, [motorsPlugin])
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    LoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning)),
    RunOptions = new RunCreationOptions
    {
        ToolConstraint = ToolConstraint.Auto,
        Temperature = 0.1f,
    },
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
        ## PERSONA
        You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
        """)),
    Arguments = new()
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    }
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
