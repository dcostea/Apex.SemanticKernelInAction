using Chatbot.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Chatbot.Services;

public interface IKernelFactory
{
    Kernel CreateKernel();
}

public sealed class KernelFactory(IOptions<AiProvidersOptions> options) : IKernelFactory
{
    public Kernel CreateKernel()
    {
        var providers = options.Value;
        var builder = Kernel.CreateBuilder();

        if (providers.UseAzureOpenAI)
        {
            builder.AddAzureOpenAIChatCompletion(
                providers.AzureOpenAI.DeploymentName,
                providers.AzureOpenAI.Endpoint,
                providers.AzureOpenAI.ApiKey);
        }
        else
        {
            builder.AddOpenAIChatCompletion(
                providers.OpenAI.ModelId,
                providers.OpenAI.ApiKey);
        }

        return builder.Build();
    }
}
