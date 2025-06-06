using ChatbotServiceSample.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

var host = Host.CreateDefaultBuilder(args)
.ConfigureServices((context, services) =>
{
    var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

    var kernel = services.AddKernel();
    services
        //.AddAzureOpenAIChatCompletion(
        //deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
        //endpoint: configuration["AzureOpenAI:Endpoint"]!,
        //apiKey: configuration["AzureOpenAI:ApiKey"]!)
        .AddOpenAIChatCompletion(
             modelId: configuration["OpenAI:ModelId"]!,
             apiKey: configuration["OpenAI:ApiKey"]!);

    services.AddHostedService<BackgroundTaskService>();
})
.Build();

await host.RunAsync();
