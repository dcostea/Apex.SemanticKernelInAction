using Chatbot.Options;
using Chatbot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services
    .AddOptions<AiProvidersOptions>()
    .Bind(builder.Configuration.GetSection("AiProviders"))
    .Configure(options =>
        builder.Configuration.GetSection("OpenAI").Bind(options.OpenAI))
    .Validate(options =>
    {
        if (options.UseAzureOpenAI)
        {
            return !string.IsNullOrWhiteSpace(options.AzureOpenAI.DeploymentName)
                && !string.IsNullOrWhiteSpace(options.AzureOpenAI.Endpoint)
                && !string.IsNullOrWhiteSpace(options.AzureOpenAI.ApiKey);
        }

        return !string.IsNullOrWhiteSpace(options.OpenAI.ModelId)
            && !string.IsNullOrWhiteSpace(options.OpenAI.ApiKey);
    }, "Missing required AI provider configuration values.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ChatbotOptions>()
    .Bind(builder.Configuration.GetSection("Chatbot"))
    .ValidateDataAnnotations()
    .Validate(options => !string.IsNullOrWhiteSpace(options.SystemPrompt), "Chatbot:SystemPrompt is required.")
    .ValidateOnStart();

builder.Services.AddSingleton<IKernelFactory, KernelFactory>();
builder.Services.AddSingleton<ConsoleChatbotRunner>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(console =>
{
    console.TimestampFormat = "HH:mm:ss ";
    console.SingleLine = true;
});

using var host = builder.Build();

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cancellationTokenSource.Cancel();
};

var runner = host.Services.GetRequiredService<ConsoleChatbotRunner>();
await runner.RunAsync(cancellationTokenSource.Token);
