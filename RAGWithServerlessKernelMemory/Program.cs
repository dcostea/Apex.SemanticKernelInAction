using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var memory = new KernelMemoryBuilder()
    .WithAzureOpenAITextGeneration(new AzureOpenAIConfig 
    {
        APIKey = configuration["AzureOpenAI:ApiKey"]!, 
        Deployment = configuration["AzureOpenAI:DeploymentName"]!,
        Endpoint = configuration["AzureOpenAI:Endpoint"]!
    })
    .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig 
    { 
        APIKey = configuration["AzureOpenAI:ApiKey"]!, 
        Deployment = configuration["AzureOpenAI:EmbeddingDeploymentName"]!,
        Endpoint = configuration["AzureOpenAI:Endpoint"]!
    })
    //.WithOpenAIDefaults(configuration["OpenAI:ApiKey"]!)
    .Build<MemoryServerless>();

await memory.ImportDocumentAsync(@"Data/weather1.txt");
await memory.ImportDocumentAsync(@"Data/weather2.pdf");

var response = await memory.AskAsync("What is the average temperature on 1st and 6th of June?");

Console.WriteLine($"RESPONSE: {response.Result}");
