using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

PersistentAgentsClient client = new(configuration["AzureOpenAIAgent:Endpoint"]!, new DefaultAzureCredential());

const string RagFilesDirectory = @"Data";
string[] filePaths = Directory.GetFiles(RagFilesDirectory);
List<string> fileIds = [];
foreach (var filePath in filePaths)
{
    string fileName = Path.GetFileName(filePath);
    using FileStream stream = File.OpenRead(filePath);
    PersistentAgentFileInfo fileInfo = await client.Files.UploadFileAsync(stream, PersistentAgentFilePurpose.Agents, fileName);
    string fileId = fileInfo.Id;
    fileIds.Add(fileId);
}

PersistentAgentsVectorStore fileStore = await client.VectorStores.CreateVectorStoreAsync(fileIds);

PersistentAgent definition = await client.Administration.CreateAgentAsync(
    configuration["AzureOpenAIAgent:DeploymentName"]!,
    tools: [new FileSearchToolDefinition()],
    toolResources: new() { FileSearch = new() { VectorStoreIds = { fileStore.Id } } });
#pragma warning disable SKEXP0110 // AzureAIAgent is experimental

AzureAIAgent agent = new(definition, client)
{
    Name = "RobotCarAgent",
    Description = "A robot car that can perform basic moves",
    Arguments = new()
    {
        ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
    },
    Template = new KernelPromptTemplateFactory()
        .Create(new PromptTemplateConfig("""
            You are an AI assistant controlling a robot car capable of performing basic moves: {{$basic_moves}}.
            You have to break down the provided complex commands into basic moves you know.
            Respond only with the permitted moves, without any additional explanations.
            """
    )),
};

var query = "There is a tree directly in front of the car. Avoid it and then come back to the original path.";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(query))
{
    Console.WriteLine(response.Message.Content);
}

await client.Administration.DeleteAgentAsync(agent.Id);
await client.VectorStores.DeleteVectorStoreAsync(fileStore.Id);
foreach (var fileId in fileIds)
{
    await client.Files.DeleteFileAsync(fileId);
}
