using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Plugins.Native;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

#pragma warning disable SKEXP0110 // OpenAIAssistantAgentFactory is experimental.
AzureAIAgentFactory factory = new();

PersistentAgentsClient client = new(configuration["AzureOpenAIAgent:Endpoint"]!, new DefaultAzureCredential());

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
builder.Services.AddSingleton(client);
var kernel = builder.Build();

kernel.ImportPluginFromType<MotorsPlugin>();

var promptTemplateFactory = new KernelPromptTemplateFactory();
var yamlContent = File.ReadAllText("AzureAIAgent.yaml");
var agent = await factory.CreateAgentFromYamlAsync(yamlContent, 
    new AgentCreationOptions() { Kernel = kernel, PromptTemplateFactory = promptTemplateFactory },
    configuration);

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent!.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}
