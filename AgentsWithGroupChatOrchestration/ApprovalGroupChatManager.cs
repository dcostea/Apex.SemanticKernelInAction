using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentsWithGroupChatOrchestration;
#pragma warning disable SKEXP0110
public sealed class ApprovalGroupChatManager : RoundRobinGroupChatManager
{
    private readonly string _approverName;
    public ApprovalGroupChatManager(string approverName)
    {
        _approverName = approverName;
    }

    public override ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
    {
        var last = history.LastOrDefault();
#pragma warning disable SKEXP0001
        bool shouldTerminate = last?.AuthorName == _approverName &&
            last.Content?.Contains("tea", StringComparison.OrdinalIgnoreCase) == true;
        return ValueTask.FromResult(new GroupChatManagerResult<bool>(shouldTerminate)
        {
            Reason = shouldTerminate ? "Approved by reviewer." : "Not yet approved."
        });
    }
}
