using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

namespace ChatbotServiceSample.Services;

public class BackgroundTaskService : BackgroundService
{
    private readonly Kernel _kernel;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public BackgroundTaskService(Kernel kernel, IHostApplicationLifetime hostApplicationLifetime)
    {
        _kernel = kernel;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Hit enter after the prompt anytime to end the conversation");
        Console.Write($"User >>> ");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var prompt = Console.ReadLine();
                if (!string.IsNullOrEmpty(prompt))
                {
                    var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: stoppingToken);

                    Console.WriteLine($" Bot >>> {response}");
                    Console.Write($"User >>> ");
                }
                else
                {
                    _hostApplicationLifetime.StopApplication();
                }
            }
        }
    }
}
