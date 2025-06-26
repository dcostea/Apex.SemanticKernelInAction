using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

const string ModelUri = "http://localhost:11434";
//const string Model = "mistral-small3.1:latest";
//const string Model = "magistral:24b";
//const string Model = "mistral-small3.2:24b";
const string Model = "qwen3:14b";

var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
builder.AddOllamaChatCompletion(
    modelId: Model,
    endpoint: new Uri(ModelUri))
.Build();
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

#pragma warning disable SKEXP0001 // RetainArgumentTypes is experimental
var executionSettings = new OllamaPromptExecutionSettings
{
    Temperature = 0.1F,
    TopP = 0.95F,
    NumPredict = 2000,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new FunctionChoiceBehaviorOptions { RetainArgumentTypes = true }),
};

ChatCompletionAgent agent = new()
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    Kernel = kernel,
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
            You have to break down the provided complex commands into basic moves you know.
            Respond only with the permitted moves, without any additional explanations.
            """)),
    Arguments = new(executionSettings)
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    },
    LoggerFactory = kernel.Services.GetRequiredService<ILoggerFactory>(),
};

var query = """
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."
    /no_think
    """;

var options = new AgentInvokeOptions
{
    AdditionalInstructions = """
    Always call a tool if it can answer the user's question.

    Available tools:
      MotorsPlugin-forward
      MotorsPlugin-backward
      MotorsPlugin-turn_left
      MotorsPlugin-turn_right
      MotorsPlugin-stop

    Do not answer directly. Only call a tool if possible.
    """
};

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(query, options: options))
{
    Console.WriteLine(response.Message.Content);
}
