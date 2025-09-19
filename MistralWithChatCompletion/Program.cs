using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.MistralAI;
using Plugins.Native;
using System.Text.RegularExpressions;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

const string Model = "mistral-small-latest";
//const string Model = "mistral-large-latest";

var builder = Kernel.CreateBuilder();
builder.AddMistralChatCompletion(
    modelId: Model,
    apiKey: configuration["Mistral:ApiKey"]!)
.Build();
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
Kernel kernel = builder.Build();

KernelPlugin motorPlugins = kernel.ImportPluginFromType<MotorsPlugin>();

MistralAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0.1F,
    TopP = 0.95F,
    MaxTokens = 2000,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(motorPlugins)
};

KernelArguments kernelArguments = new(executionSettings)
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

var systemPrompt = """
    You are an AI assistant controlling a robot car.
    Your task is to break down complex commands into a sequence for basic moves such as {{$basic_moves}}.
    YOU MUST ONLY USE THE PROVIDED FUNCTIONS (MotorsPlugin.forward, MotorsPlugin.backward, MotorsPlugin.turn_left, MotorsPlugin.turn_right, MotorsPlugin.stop).
    DO NOT respond with text descriptions - ONLY use function calls with their arguments.
    """;
var userPrompt = """
    Complex command:
    "{{$input}}"
    """;
var promptTemplateFactory = new KernelPromptTemplateFactory();
var systemPromptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(systemPrompt));
var renderedSystemPrompt = await systemPromptTemplate.RenderAsync(kernel, kernelArguments);
var userPromptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(userPrompt));
var renderedUserPrompt = await userPromptTemplate.RenderAsync(kernel, kernelArguments);
ChatHistory chatHistory = new(renderedSystemPrompt);
chatHistory.AddUserMessage($"{renderedUserPrompt}");

var chat = kernel.GetRequiredService<IChatCompletionService>();

var response = await chat.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response}\n");

if (!chatHistory.Any(m => m.Role == AuthorRole.Tool))
{
    Console.WriteLine("NO FUNCTION CALLS FOUND IN THE RESPONSE, CALLING FUNCTION CALLING FIX!");
    await FunctionCallingFix(kernel, motorPlugins, response);
}

static async Task FunctionCallingFix(Kernel kernel, KernelPlugin motorPlugin, ChatMessageContent response)
{
    if (string.IsNullOrEmpty(response.ToString())) return;
    
    var matches = new Regex(@"MotorsPlugin\.(\w+)\s*\(\s*(\d+(?:\.\d+)?)?\s*\)", RegexOptions.IgnoreCase)
        .Matches(response.ToString());
    
    if (matches.Count == 0)
    {
        Console.WriteLine("No function calls found in the response");
        return;
    }
    
    foreach (Match match in matches)
    {
        string functionName = match.Groups[1].Value;
        
        try
        {
            if (!motorPlugin.TryGetFunction(functionName, out var function))
            {
                Console.WriteLine($"Function '{functionName}' not found in MotorsPlugin");
                continue;
            }
            
            var args = function.Metadata.Parameters.Count > 0 
                ? new KernelArguments { [function.Metadata.Parameters[0].Name] = string.IsNullOrEmpty(match.Groups[2].Value) ? "90" : match.Groups[2].Value }
                : [];
                
            var result = await kernel.InvokeAsync(function, args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing function {functionName}: {ex.Message}");
        }
    }
}
