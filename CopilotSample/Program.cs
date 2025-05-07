using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernel = Kernel.CreateBuilder()
//.AddAzureOpenAIChatCompletion(
//deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//endpoint: configuration["AzureOpenAI:Endpoint"]!,
//apiKey: configuration["AzureOpenAI:ApiKey"]!)
.AddOpenAIChatCompletion(
     modelId: configuration["OpenAI:ModelId"]!,
     apiKey: configuration["OpenAI:ApiKey"]!)
.Build();

var commandsPluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "CommandsPlugin");
kernel.ImportPluginFromPromptDirectory(commandsPluginPath, "CommandsPlugin");
//kernel.ImportPluginFromType<RobotCarPlugin>();

var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    """
);

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

while (true)
{
    Console.Write(" User >>> ");
    var prompt = Console.ReadLine(); // There is a tree directly in front of the car. Avoid it and then come back to the original path.
    if (string.IsNullOrEmpty(prompt)) break;

    history.AddUserMessage(prompt);

    var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

    Console.WriteLine($"  Bot >>> {response}");

    history.Add(response);
}
