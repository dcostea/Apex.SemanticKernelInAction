using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.Text.Json;

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

// Define paths
var promptFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Functions", "BreakdownComplexCommand", "skprompt.txt");
var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Functions", "BreakdownComplexCommand", "config.json");

// Read prompt content
var promptContent = File.ReadAllText(promptFilePath);

// Read and parse config.json
var configJson = File.ReadAllText(configFilePath);
var configOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var config = PromptTemplateConfig.FromJson(configJson);

// Create the function with both prompt and config
var promptFunctionFromSkPrompt = kernel.CreateFunctionFromPrompt(promptContent, config.ExecutionSettings["default"]);

Console.WriteLine($"""
    SEMANTIC FUNCTION:
      Name: {promptFunctionFromSkPrompt.Name}
      Description: '{promptFunctionFromSkPrompt.Description}'
      Plugin name: '{promptFunctionFromSkPrompt.PluginName}'
      Execution settings: {string.Join(" ", promptFunctionFromSkPrompt.ExecutionSettings["default"].ExtensionData)}");
      Input variable: {string.Join("", promptFunctionFromSkPrompt.Metadata.Parameters.Select(p => $"\n    {p.Name} : {p.ParameterType!.Name} {(p.IsRequired ? "required" : "")} '{p.Description}'"))}
      Output variable: {promptFunctionFromSkPrompt.Metadata.ReturnParameter.Schema} {promptFunctionFromSkPrompt.Metadata.ReturnParameter.ParameterType} '{promptFunctionFromSkPrompt.Metadata.ReturnParameter.Description}'
    """);

var kernelArguments = new KernelArguments()
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

// Querying the prompt function
var response = await promptFunctionFromSkPrompt.InvokeAsync(kernel, kernelArguments);

Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}"); // shows the rendered prompt of the prompt function
Console.WriteLine($"PROMPT RESPONSE: {response}");
