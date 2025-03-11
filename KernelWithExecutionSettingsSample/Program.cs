using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

#pragma warning disable SKEXP0010 // Logprobs, and TopLogprobs are experimental and they needs to be enabled explicitly
var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
{
    MaxTokens = 100, // maximum number of tokens to generate, the excess will be truncated
    Temperature = 0.9,  // low temperature reduces the randomness of the generated tokens
    Logprobs = true, // Logprobs instructs the model to populate the logarithmic probabilities in the response
    TopLogprobs = 10, // TopLogprobs instructs the model how many likely tokens (with its logarithmic probabilities) to return for each token
    ChatSystemPrompt = """
        You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the moves, without any additional explanations.
        """ // the system prompt tailors the model behaviour
});

var response = await kernel.InvokePromptAsync("""
    Choose a random basic movement.
    Respond only with the movement name alone.
    """, // the user prompt which changes which each new query
    kernelArguments);

Console.WriteLine($"RESPONSE: {response.GetValue<string>()}");

// logarithmic probabilities are experimental and may not be available in the future, but they are useful to understand how the model predicts tokens
// we can notice that the next token has its probability recomputed after each token prediction
var logProbs = response.Metadata!["ContentTokenLogProbabilities"] as List<ChatTokenLogProbabilityDetails>;
if (logProbs is not null)
{
    foreach (var logProb in logProbs!)
    {
        // show logarithmic probabilities for candidate tokens
        Console.WriteLine("Top logarithmic probabilities:");
        foreach (var topLogProb in logProb.TopLogProbabilities!)
        {
            // show top logarithmic probabilities for each token
            Console.WriteLine($"  {topLogProb.Token} ({topLogProb.LogProbability})");
        }
    }
}
