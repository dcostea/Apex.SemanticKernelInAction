using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using Plugins;

namespace CopilotWithSemanticFunctionSample;

internal class Program
{
    static async Task Main(string[] args)
    {
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

        kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "CommandsPlugin"), "CommandsPlugin");
        kernel.ImportPluginFromType<RobotCarPlugin>();

        #pragma warning disable SKEXP0001 // FunctionChoiceBehavior is experimental and it needs to be enabled explicitly

        var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
        {
            Temperature = 0.1,
            ChatSystemPrompt = """
                    You are an AI assistant controlling a robot car.
                    """,

            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        });

        while (true)
        {
            Console.Write(" User >>> ");
            var prompt = Console.ReadLine(); // You have a tree in front of the car. Avoid it and then resume the initial direction.
            if (string.IsNullOrEmpty(prompt)) break;

            var response = await kernel.InvokePromptAsync(prompt, kernelArguments);

            Console.WriteLine($"  Bot >>> {response}");
        }
    }
}
