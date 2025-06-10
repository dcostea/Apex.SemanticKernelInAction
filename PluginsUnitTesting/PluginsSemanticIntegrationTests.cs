﻿using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PluginsUnitTesting;

public class PluginsSemanticIntegrationTests
{
    [Fact]
    public async Task SemanticPromptIntegrationTest()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<PluginsSemanticIntegrationTests>().Build();

        #pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var builder = Kernel.CreateBuilder();
        //builder.AddAzureOpenAIChatCompletion(
        //    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
        //    endpoint: configuration["AzureOpenAI:Endpoint"]!,
        //    apiKey: configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddAzureOpenAIEmbeddingGenerator(
        //    deploymentName: "text-embedding-ada-002",
        //    modelId: "text-embedding-ada-002",
        //    endpoint: configuration["AzureOpenAI:Endpoint"]!,
        //    apiKey: configuration["AzureOpenAI:ApiKey"]!);
        builder.AddOpenAIChatCompletion(
            modelId: configuration["OpenAI:ModelId"]!,
            apiKey: configuration["OpenAI:ApiKey"]!);
        builder.AddOpenAIEmbeddingGenerator(
            modelId: configuration["OpenAI:EmbeddingModelId"]!,
            apiKey: configuration["OpenAI:ApiKey"]!);
        //builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
        var kernel = builder.Build();

        var MinSimilarity = 0.70f; // Adjust based on your quality needs
        var expectedResponse = "Semantic Kernel (SK) is an open-source SDK that combines AI services like OpenAI with programming languages like C#";

        // Create and invoke the function for provided prompt
        var function = kernel.CreateFunctionFromPrompt("Explain Semantic Kernel in one sentence");
        var actualResponse = await kernel.InvokeAsync<string>(function);
        
        // Compute the similarity for both expected and actual response
        var similarity = await CalculateSimilarity(kernel, expectedResponse, actualResponse!);

        // The similarity should be above the suggested threshold MinSimilarity
        Assert.True(similarity >= MinSimilarity, $"Similarity score {similarity:P0} below threshold {MinSimilarity:P0}");
    }

    private static async Task<float> CalculateSimilarity(Kernel kernel, string string1, string string2)
    {
        // Get the text embedding generator
        var textEmbeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        // Generate embeddings for both strings
        var embedding1 = await textEmbeddingGenerator.GenerateAsync([ string1 ]);
        var embedding2 = await textEmbeddingGenerator.GenerateAsync([ string2 ]);

        return CosineSimilarity(embedding1[0].Vector, embedding2[0].Vector);
    }

    private static float CosineSimilarity(ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2)
    {
        float dotProduct = 0, magnitude1 = 0, magnitude2 = 0;
        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1.Span[i] * embedding2.Span[i];
            magnitude1 += embedding1.Span[i] * embedding1.Span[i];
            magnitude2 += embedding2.Span[i] * embedding2.Span[i];
        }
        return dotProduct / (MathF.Sqrt(magnitude1) * MathF.Sqrt(magnitude2));
    }
}
