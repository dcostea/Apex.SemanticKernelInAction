using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using CopilotSampleWithSemanticFunction.Plugins.MotorPlugin;

namespace CopilotSampleWithSemanticFunction;

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

        //kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "MotorPlugin"), "BreakdownComplexCommands");
        kernel.ImportPluginFromType<MotorCommands>();

        while (true)
        {
            Console.Write(" User >>> ");
            var prompt = Console.ReadLine(); // You have a tree in front of the car. Avoid it and then resume the initial direction.

            if (string.IsNullOrEmpty(prompt))
            {
                break;
            }
#pragma warning disable SKEXP0001

            var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
            {
                ChatSystemPrompt = """
                    You are an AI assistant controlling a robot car.
                    """,
                //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            });

            var response = await kernel.InvokePromptAsync(prompt, kernelArguments);

            Console.WriteLine($"  Bot >>> {response}");
        }
    }
}

