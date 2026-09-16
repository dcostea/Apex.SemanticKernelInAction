using System.ComponentModel.DataAnnotations;

namespace Chatbot.Options;

public sealed class AiProvidersOptions
{
    public bool UseAzureOpenAI { get; init; }

    [Required]
    public OpenAIOptions OpenAI { get; init; } = new();

    [Required]
    public AzureOpenAIOptions AzureOpenAI { get; init; } = new();
}

public sealed class OpenAIOptions
{
    public string ModelId { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}

public sealed class AzureOpenAIOptions
{
    public string DeploymentName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}
