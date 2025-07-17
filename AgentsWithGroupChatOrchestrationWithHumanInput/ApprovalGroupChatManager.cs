using AgentsWithConcurrentOrchestration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentsWithGroupChatOrchestration;

public sealed class ApprovalGroupChatManager : RoundRobinGroupChatManager
{
    private readonly OrchestrationMonitor _monitor;

    public ApprovalGroupChatManager(OrchestrationMonitor monitor)
    {
        _monitor = monitor;
    }

    public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default)
    {
        string? lastAgent = history.LastOrDefault()?.AuthorName;

        if (lastAgent is null)
        {
            return ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "Agent name is missing!" });
        }

        if (lastAgent == "MotorsAgent")
        {
            return ValueTask.FromResult(new GroupChatManagerResult<bool>(true) { Reason = "User input is needed." });
        }

        return ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "User input is not needed." });
    }

    public override ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
        ChatHistory history, CancellationToken cancellationToken = default)
    {
        var lastMessage = history?.Skip(1).LastOrDefault();

        bool isApprovedByAgent = _monitor.IsApproved;

        bool? isApprovedByHuman = null;
        if (lastMessage?.Role == AuthorRole.User)
        {
            var containsApproved = lastMessage?.Content?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) == true;
            var containsNotApproved = lastMessage?.Content?.Contains("NOT APPROVED", StringComparison.InvariantCultureIgnoreCase) == true;

            if (containsApproved && !containsNotApproved)
            {
                isApprovedByHuman = true;
            }

            if (containsNotApproved)
            {
                isApprovedByHuman = false;
            }
        }

        string approvalState = isApprovedByHuman ?? isApprovedByAgent
            ? "[APPROVED]" 
            : "[NOT APPROVED]";

        if (isApprovedByHuman ?? isApprovedByAgent)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(approvalState);
            Console.ResetColor();
        }

        return ValueTask.FromResult(new GroupChatManagerResult<bool>(isApprovedByHuman ?? isApprovedByAgent)
        {
            Reason = approvalState
        });
    }
}
