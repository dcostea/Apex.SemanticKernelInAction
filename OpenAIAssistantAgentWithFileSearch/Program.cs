using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Assistants;
using System.ClientModel;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var assistantClient = new AssistantClient(configuration["OpenAI:ApiKey"]!);
var assistant = await assistantClient.CreateAssistantAsync(
    configuration["OpenAI:ModelId"]!,
    enableFileSearch: true);

var openAIClient = OpenAIAssistantAgent.CreateOpenAIClient(new ApiKeyCredential(configuration["OpenAI:ApiKey"]!));

const string RagFilesDirectory = @"Data";
string[] filePaths = Directory.GetFiles(RagFilesDirectory);
List<string> fileIds = [];
foreach (var filePath in filePaths)
{
    string fileName = Path.GetFileName(filePath);
    using FileStream stream = File.OpenRead(filePath);
    string fileId = await openAIClient.UploadAssistantFileAsync(stream, fileName);
    fileIds.Add(fileId);
}
string vectorStoreId = await openAIClient.CreateVectorStoreAsync(fileIds, waitUntilCompleted: true);

OpenAIAssistantAgentThread thread = new (assistantClient, vectorStoreId: vectorStoreId);

OpenAIAssistantAgent agent = new(assistant, assistantClient)
{
    Name = "RobotCarAgent",
    Instructions = """
        You are an assistant who responds including citations to the relevant information where it is referenced in the response.
        """,
    Description = "A robot car assistant.",
    LoggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace)),
};

var query = "What was the average temperature on 2nd of June?";

Console.WriteLine("RESPONSE: ");
await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync(query, thread))
{
    Console.WriteLine(response.Message.Content);
}

await thread.DeleteAsync();
await openAIClient.DeleteVectorStoreAsync(vectorStoreId);
foreach (var fileId in fileIds)
{
    await openAIClient.DeleteFileAsync(fileId);
}
