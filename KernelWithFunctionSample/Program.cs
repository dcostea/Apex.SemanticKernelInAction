using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using System.ComponentModel;

namespace KernelWithFunctionSample;

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

        var prompt = """
            Your task is to break down complex commands into a sequence basic moves such as {{$basic_moves}}.
            Provide only the sequence of the basic movements, without any additional explanations.

            Complex command:
            {{$input}}.
            """;

        // Preparing the prompt function from plain text (not fully packed with all settings)
        var promptFunctionFromPrompt = kernel.CreateFunctionFromPrompt(prompt);

        // Preparing the prompt function
        var promptTemplateConfig = new PromptTemplateConfig 
        {
            TemplateFormat = "semantic-kernel",
            Name = "BreakdownComplexCommands",
            Description = "It breaks down the given complex command into a step-by-step sequence of basic moves.",
            Template = prompt
        };
        var promptFunction = kernel.CreateFunctionFromPrompt(promptTemplateConfig); // create a prompt function from PromptTemplateConfig object with name, decription and execution settings

        // Preparing the kernel arguments with execution settings
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.1
        };
        var kernelArguments = new KernelArguments(executionSettings) 
        {
            ["input"] = "There is a tree directly in front of the car. Avoid it and then resume the initial direction.",
            ["basic_moves"] = "forward, backward, turn left, turn right, and stop" 
        };

        #pragma warning disable SKEXP0001 // RenderedPrompt is experimental and it needs to be enabled explicitly

        // Querying the prompt function
        var promptResponse = await promptFunction.InvokeAsync(kernel, kernelArguments);
        Console.WriteLine($"RENDERED PROMPT: {promptResponse.RenderedPrompt}"); // shows the rendered prompt of the prompt function
        Console.WriteLine($"PROMPT RESPONSE: {promptResponse}");

        // Preparing the method function
        var methodFunction = kernel.CreateFunctionFromMethod(() => DateTime.UtcNow.ToString("F"), "GetCurrentUtcTime", "Retrieves the current time in UTC format.");

        // Querying the method function
        var methodResponse = await methodFunction.InvokeAsync(kernel);
        Console.WriteLine($"RENDERED METHOD: {methodResponse.RenderedPrompt}"); // shows the rendered prompt of the method function
        Console.WriteLine(methodResponse.GetValue<string>());
        Console.WriteLine($"METHOD RESPONSE: {methodResponse}");
    }
}