using Microsoft.Extensions.Configuration;
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

var chat = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory("You are an assistant specialised in named entities extraction.");

history.AddUserMessage(
[
    new TextContent("What data can you read in the image? Respond with JSON object"),
    new ImageContent(new Uri("https://apexcode.ro/restaurant-receipt.jpg"))
]);

var response = await chat.GetChatMessageContentsAsync(history);

Console.WriteLine(response.First());
