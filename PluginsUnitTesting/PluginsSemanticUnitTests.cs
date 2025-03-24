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
        // Define the expected response and the expected rendered prompt
        string expectedResponse = "Expected output based on the prompt template and input.";
        string expectedRenderedPrompt = "Answer the following question: What is the current weather in London?";
;
        // Setup a mock chat completion service that will return the baseline output.
        var mockChatService = new Mock<IChatCompletionService>();
        mockChatService.Setup(s => s.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync([new ChatMessageContent { Content = expectedResponse }]);

        // Register the mocked service into the kernel.
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockChatService.Object);
        var kernelBuilder = Kernel.CreateBuilder();
        foreach (var service in serviceCollection)
        {
            kernelBuilder.Services.Add(service);
        }
        var kernel = kernelBuilder.Build();

        // Create a semantic function with a specified prompt template.
        var promptTemplate = "Answer the following question: {{$input}}";
        var semanticFunction = kernel.CreateFunctionFromPrompt(promptTemplate);

        // Invoke the semantic function using a specific input.
        var input = new KernelArguments
        {
            ["input"] = "What is the current weather in London?"
        };
        var actualResponse = await semanticFunction.InvokeAsync(kernel, input);

        // The test will pass if the response exactly matches the expectedResponse and the expectedRenderedPropmt matches the response rendered prompt
        Assert.Equal(expectedRenderedPrompt, actualResponse.RenderedPrompt);
        Assert.Equal(expectedResponse, actualResponse.GetValue<string>());

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