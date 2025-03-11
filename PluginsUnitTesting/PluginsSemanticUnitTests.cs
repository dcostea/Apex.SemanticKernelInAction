using Moq;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;

namespace PluginsUnitTesting;

public class PluginsSemanticUnitTests
{
    [Fact]
    public async Task SemanticPromptUnitTest()
    {
        // Define a baseline output that you expect for the given prompt.
        string baselineOutput = "Expected output based on the prompt template and input.";

        // Setup a mock chat completion service that will return the baseline output.
        var mockChatService = new Mock<IChatCompletionService>();
        mockChatService.Setup(s => s.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync([new ChatMessageContent { Content = baselineOutput }]);

        // Register the mocked service into the kernel.
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockChatService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Use the service provider to resolve the kernel's services.
        var kernelBuilder = Kernel.CreateBuilder();
        foreach (var service in serviceCollection)
        {
            kernelBuilder.Services.Add(service);
        }
        var kernel = kernelBuilder.Build();

        // Create a semantic function with a specified prompt template.
        // The template includes a placeholder for input that will be interpolated at runtime.
        var promptTemplate = "Answer the following question: {input}";
        var semanticFunction = kernel.CreateFunctionFromPrompt(promptTemplate);

        // Invoke the semantic function using a specific input.
        var input = new KernelArguments
        {
            ["input"] = "What is the current weather in London?"
        };
        var actualOutput = await semanticFunction.InvokeAsync<string>(kernel, input);

        // The test will pass if the output exactly matches the baseline.
        Assert.Equal(baselineOutput, actualOutput);

        // Optionally, verify that the mock completion service was called with expected parameters.
        mockChatService.Verify(
            service => service.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
} 