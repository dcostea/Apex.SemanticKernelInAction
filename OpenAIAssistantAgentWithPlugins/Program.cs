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
var assistant = await assistantClient.CreateAssistantAsync(configuration["OpenAI:ModelId"]);

var motorsPlugin = KernelPluginFactory.CreateFromType<MotorsPlugin>();

OpenAIAssistantAgent agent = new(assistant, assistantClient, [motorsPlugin])
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    LoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information)),
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
            You have to break down the provided complex commands into basic moves you know.
            Respond only with the permitted moves, without any additional explanations.
            """)),
    RunOptions = new RunCreationOptions { ToolConstraint = ToolConstraint.Auto },
    Arguments = new()
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    }
};

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}
