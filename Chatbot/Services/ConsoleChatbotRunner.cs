using Chatbot.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Chatbot.Services;

public sealed class ConsoleChatbotRunner(
    IKernelFactory kernelFactory,
    IOptions<ChatbotOptions> chatbotOptions,
    ILogger<ConsoleChatbotRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var options = chatbotOptions.Value;
        var kernel = kernelFactory.CreateKernel();

        var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
        {
            Temperature = options.Temperature,
            ChatSystemPrompt = options.SystemPrompt
        });

        Console.WriteLine("Type a command for the car. Use 'exit' to quit.");

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write(" User >>> ");
            var prompt = Console.ReadLine();

            if (prompt is null)
            {
                break;
            }

            prompt = prompt.Trim();
            if (prompt.Length == 0)
            {
                continue;
            }

            if (options.ExitCommands.Any(command => prompt.Equals(command, StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }

            try
            {
                logger.LogInformation("Invoking model. PromptLength={PromptLength}", prompt.Length);

                var response = await kernel.InvokePromptAsync(prompt, kernelArguments, cancellationToken: cancellationToken);
                Console.WriteLine($"  Bot >>> {response.ToString().Trim()}");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Model invocation failed.");
                Console.WriteLine("  Bot >>> Request failed. Please retry.");
            }
        }

        logger.LogInformation("Chatbot session ended.");
    }
}
