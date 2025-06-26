using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace AgentsWithGroupChatOrchestrationAndManager;

#pragma warning disable SKEXP0110
public sealed class AIGroupChatManager(string topic, IChatCompletionService chatCompletion) : GroupChatManager
{
    private static class Prompts
    {
        public static string Termination(string topic) =>
            $"""
                You are mediator that guides a discussion on the topic of '{topic}'. 
                You need to determine if the discussion has reached a conclusion. 
                If you would like to end the discussion, please respond with True. Otherwise, respond with False.
                """;

        public static string Selection(string topic, string participants) =>
            $"""
                You are mediator that guides a discussion on the topic of '{topic}'. 
                You need to select the next participant to speak. 
                Here are the names and descriptions of the participants: 
                {participants}\n
                Please respond with only the name of the participant you would like to select.
                """;

        public static string Filter(string topic) =>
            $"""
                You are mediator that guides a discussion on the topic of '{topic}'. 
                You have just concluded the discussion. 
                Please summarize the discussion and provide a closing statement.
                """;
    }

    /// <inheritdoc/>
    public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default) =>
        GetResponseAsync<string>(history, Prompts.Filter(topic), cancellationToken);

    /// <inheritdoc/>
    public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default) =>
        GetResponseAsync<string>(history, Prompts.Selection(topic, team.FormatList()), cancellationToken);

    /// <inheritdoc/>
    public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "The AI group chat manager does not request user input." });

    /// <inheritdoc/>
    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
    {
        GroupChatManagerResult<bool> result = await base.ShouldTerminate(history, cancellationToken);
        if (!result.Value)
        {
            result = await GetResponseAsync<bool>(history, Prompts.Termination(topic), cancellationToken);
        }
        return result;
    }

    private async ValueTask<GroupChatManagerResult<TValue>> GetResponseAsync<TValue>(ChatHistory history, string prompt, CancellationToken cancellationToken = default)
    {
        OpenAIPromptExecutionSettings executionSettings = new() { ResponseFormat = typeof(GroupChatManagerResult<TValue>) };
        ChatHistory request = [.. history, new ChatMessageContent(AuthorRole.System, prompt)];
        ChatMessageContent response = await chatCompletion.GetChatMessageContentAsync(request, executionSettings, kernel: null, cancellationToken);
        string responseText = response.ToString();
        return
            JsonSerializer.Deserialize<GroupChatManagerResult<TValue>>(responseText) ??
            throw new InvalidOperationException($"Failed to parse response: {responseText}");
    }
}
