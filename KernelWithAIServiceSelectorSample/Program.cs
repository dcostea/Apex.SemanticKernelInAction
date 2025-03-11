using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Services;
using System.Diagnostics.CodeAnalysis;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint"]!,
    apiKey: configuration["AzureOpenAI:ApiKey"]!,
    serviceId: "OPENAI");
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!,
    serviceId: "AZURE");
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
builder.Services.AddSingleton<IAIServiceSelector>(new CustomAIServiceSelector("AZURE"));
var kernel = builder!.Build();

var prompt = """
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.
    
    There is a tree directly in front of the car. Avoid it and then come back to the original path.
    """;

var response = await kernel.InvokePromptAsync(prompt);

sealed class CustomAIServiceSelector(string serviceKey) : IAIServiceSelector
{
    public bool TrySelectAIService<T>(
        Kernel kernel,
        KernelFunction function,
        KernelArguments arguments,
        [NotNullWhen(true)] out T? service,
        out PromptExecutionSettings? serviceSettings)
    where T : class, IAIService
    {
        serviceSettings = new PromptExecutionSettings();

        try
        {
            service = kernel.GetRequiredService<T>(serviceKey);
            Console.WriteLine($"'{serviceKey}' AI service FOUND!");
            return true;
        }
        catch (Exception ex)
        {
            service = kernel.GetAllServices<T>().FirstOrDefault();

            if (service is null)
            {
                Console.WriteLine($"'{serviceKey}' AI service NOT found!\nException: {ex.Message}");
                return false;
            }

            Console.WriteLine($"'{serviceKey}' AI service NOT FOUND, falling back to: {service.GetType().Name}\nException: {ex.Message}");
            return true;
        }
    }
}
