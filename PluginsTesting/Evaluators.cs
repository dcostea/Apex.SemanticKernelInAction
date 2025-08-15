using Evaluation.Evaluators;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PluginsTesting;

public class Evaluators
{
    [Fact]
    public async Task CoherenceEvaluator_CoherentResponse()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();

        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, """
            1. turn right (90°)
            2. forward (1 meter)
            3. turn left (90°)
            4. forward (1 meter)
            5. turn left (90°)
            6. forward (1 meter)
            7. turn right (90°)            
            """));

        var coherenceEvaluator = new CoherenceEvaluator();
        EvaluationResult result = await coherenceEvaluator.EvaluateAsync(history, response, new ChatConfiguration(chat));
        NumericMetric coherence = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);

        Assert.Null(coherence.Interpretation!.Reason);
        Assert.True(coherence.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.False(coherence.Interpretation!.Failed);
    }

    [Fact]
    public async Task CoherenceEvaluator_IncoherentResponse()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();

        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, """
            Turn left because trees are green and I like the color, 
            forward 2 steps but watch for rain clouds, stop if you hear music, 
            backward might work better on Tuesdays when the moon is full, 
            the car needs to turn right eventually but first check your shoes.
            """));

        var coherenceEvaluator = new CoherenceEvaluator();
        EvaluationResult result = await coherenceEvaluator.EvaluateAsync(history, response, new ChatConfiguration(chat));
        NumericMetric coherence = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);

        Assert.NotNull(coherence.Interpretation!.Reason);
        Assert.False(coherence.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.True(coherence.Interpretation!.Failed);
    }

    [Fact]
    public async Task RelevanceEvaluator_RelevantResponse()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();

        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, """
            1. turn right (90°)
            2. forward (1 meter)
            3. turn left (90°)
            4. forward (1 meter)
            5. turn left (90°)
            6. forward (1 meter)
            7. turn right (90°)            
            """));

        var relevanceEvaluator = new RelevanceEvaluator();
        EvaluationResult result = await relevanceEvaluator.EvaluateAsync(history, response, new ChatConfiguration(chat));
        NumericMetric relevance = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);

        Assert.Null(relevance.Interpretation!.Reason);
        Assert.True(relevance.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.False(relevance.Interpretation!.Failed);
    }

    [Fact]
    public async Task RelevanceEvaluator_IrelevantResponse()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();

        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, """
            1. forward (1 meter)
            2. stop  
            3. forward (100 meter)
            """));

        var relevanceEvaluator = new RelevanceEvaluator();
        EvaluationResult result = await relevanceEvaluator.EvaluateAsync(history, response, new ChatConfiguration(chat));
        NumericMetric relevance = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);

        Assert.NotNull(relevance.Interpretation!.Reason);
        Assert.False(relevance.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.True(relevance.Interpretation!.Failed);
    }

    [Fact]
    public async Task GroundednessEvaluator_GroundedResponse()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();

        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are very basic.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, """
            1. turn right (90°)
            2. forward (1 meter)
            3. turn left (90°)
            4. forward (1 meter)
            5. turn left (90°)
            6. forward (1 meter)
            7. turn right (90°)            
            """));

        var baselineResponseForGroundedness =
            new GroundednessEvaluatorContext(
                """
                The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
                """);

        var groundednessEvaluator = new GroundednessEvaluator();
        EvaluationResult result = await groundednessEvaluator.EvaluateAsync(
            history,
            response,
            new ChatConfiguration(chat),
            [baselineResponseForGroundedness]);
        NumericMetric groundedness = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);

        Assert.Null(groundedness.Interpretation!.Reason);
        Assert.True(groundedness.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.False(groundedness.Interpretation!.Failed);
    }

    [Fact]
    public async Task GroundednessEvaluator_UngroundedResponse()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();

        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are very basic.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, """
            1. go round
            2. retreat
            3. jump over the tree
            4. evazive maneveur
            """));

        var baselineResponseForGroundedness =
            new GroundednessEvaluatorContext(
                """
                The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
                """);

        var groundednessEvaluator = new GroundednessEvaluator();
        EvaluationResult result = await groundednessEvaluator.EvaluateAsync(
            history,
            response,
            new ChatConfiguration(chat),
            [baselineResponseForGroundedness]);
        NumericMetric groundedness = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);

        Assert.NotNull(groundedness.Interpretation!.Reason);
        Assert.False(groundedness.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.True(groundedness.Interpretation!.Failed);
    }

    [Fact]
    public async Task CompositeEvaluator_ResponseOk()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();
        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are very basic.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        ChatResponse response = new(new ChatMessage(ChatRole.Assistant, """
            1. turn right (90°)
            2. forward (1 meter)
            3. turn left (90°)
            4. forward (1 meter)
            5. turn left (90°)
            6. forward (1 meter)
            7. turn right (90°)            
            """));

        var coherenceEvaluator = new CoherenceEvaluator();
        var equivalenceEvaluator = new EquivalenceEvaluator();
        var compositeEvaluator = new CompositeEvaluator(coherenceEvaluator, equivalenceEvaluator);

        var baselineResponseForEquivalence =
            new EquivalenceEvaluatorContext(
                """
                1. turn right
                2. forward
                3. turn left
                4. forward
                5. turn left
                6. forward
                7. turn right
                """);

        EvaluationResult result = await compositeEvaluator.EvaluateAsync(
            history,
            response,
            new ChatConfiguration(chat),
            [baselineResponseForEquivalence]);

        NumericMetric coherence = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
        NumericMetric equivalence = result.Get<NumericMetric>(EquivalenceEvaluator.EquivalenceMetricName);

        Assert.Null(coherence.Interpretation!.Reason);
        Assert.True(coherence.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.False(coherence.Interpretation!.Failed);

        Assert.Null(equivalence.Interpretation!.Reason);
        Assert.True(equivalence.Interpretation!.Rating >= EvaluationRating.Good);
        Assert.False(equivalence.Interpretation!.Failed);
    }

    [Fact]
    public async Task BasicMovesEvaluator_ValidMoves_ReturnsOk()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();
        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are very basic.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        // Valid response with only basic moves
        ChatResponse validResponse = new(new ChatMessage(ChatRole.Assistant,
            """
        1. turn right (90°)
        2. forward (1 meter)
        3. turn left (90°)
        4. forward (1 meter)
        5. stop
        """));

        var basicMovesEvaluator = new BasicMovesEvaluator();
        EvaluationResult result = await basicMovesEvaluator.EvaluateAsync(history, validResponse, new ChatConfiguration(chat));

        BooleanMetric basicMoves = result.Get<BooleanMetric>(BasicMovesEvaluator.BasicMovesMetricName);

        Assert.False(basicMoves.Interpretation!.Failed); // Should pass - only valid moves
        Assert.True(basicMoves.Value); // Should be true
        Assert.Contains("valid basic moves", basicMoves.Interpretation!.Reason);
    }

    [Fact]
    public async Task BasicMovesEvaluator_InvalidMoves_ReturnsFailed()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Evaluators>().Build();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            configuration["AzureOpenAI:DeploymentName"]!,
            configuration["AzureOpenAI:Endpoint"]!,
            configuration["AzureOpenAI:ApiKey"]!);
        //builder.AddOpenAIChatCompletion(
        //    configuration["OpenAI:ModelId"]!,
        //    configuration["OpenAI:ApiKey"]!);
        var kernel = builder.Build();

        var chat = kernel.GetRequiredService<IChatCompletionService>().AsChatClient();
        List<ChatMessage> history =
        [
            new(
                ChatRole.System,
                """
                You are an AI assistant controlling a robot car.
                The available robot car permitted moves are very basic.
                """),
            new(
                ChatRole.User,
                """
                You have to break down the provided complex commands into basic moves you know.
                Respond only with the permitted moves, without any additional explanations.
                
                Complex command:
                "There is a tree directly in front of the car. Avoid it and then come back to the original path."
                """)
        ];

        // Invalid response with non-basic moves
        ChatResponse invalidResponse = new(new ChatMessage(ChatRole.Assistant,
            """
        1. jump over the obstacle
        2. roll to the side
        3. slide under it
        4. forward (1 meter)
        5. stop
        """));

        var basicMovesEvaluator = new BasicMovesEvaluator();
        EvaluationResult result = await basicMovesEvaluator.EvaluateAsync(history, invalidResponse, new ChatConfiguration(chat));

        BooleanMetric basicMoves = result.Get<BooleanMetric>(BasicMovesEvaluator.BasicMovesMetricName);

        Assert.True(basicMoves.Interpretation!.Failed); // Should fail - contains invalid moves
        Assert.False(basicMoves.Value); // Should be false
        Assert.Contains("invalid moves", basicMoves.Interpretation!.Reason);
        Assert.Contains("jump", basicMoves.Interpretation!.Reason);
    }
}