using AgentsWithConcurrentOrchestration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentsWithGroupChatOrchestration;

public class MonitoredApprovalGroupChatManager(OrchestrationMonitor monitor) : RoundRobinGroupChatManager
{
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

    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
        ChatHistory history, CancellationToken cancellationToken = default)
    {
        // We can skip the very first user message in the history, which is the initial INPUT
        var lastUserMessage = history.Skip(1)?.LastOrDefault(h => h.Role == AuthorRole.User)?.Content;
        var isApprovedByHuman = lastUserMessage?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) ?? false;
        string approvalState = isApprovedByHuman || monitor.IsApproved
            ? "[APPROVED]" 
            : "[DENIED]";

        string stateMessage = $"State[{InvocationCount}]: {approvalState}";
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(stateMessage);
        Console.ResetColor();

        // Approval termination
        if ((monitor.IsApproved || isApprovedByHuman) && history.LastOrDefault()?.AuthorName == "MotorsAgent")
        {
            var terminationMessage = $"Termination: {approvalState}";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(terminationMessage);
            Console.ResetColor();

            return await ValueTask.FromResult(new GroupChatManagerResult<bool>(true)
            {
                Reason = terminationMessage
            });
        }

        // Maximum invocation count termination
        var shouldTerminate = await base.ShouldTerminate(history, cancellationToken);

        if (shouldTerminate.Value)
        {
            var terminationMessage = $"Termination: Maximum number of invocations ({MaximumInvocationCount}) reached.";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(terminationMessage);
            Console.ResetColor();

            return await ValueTask.FromResult(new GroupChatManagerResult<bool>(true)
            {
                Reason = terminationMessage
            });
        }

        return await ValueTask.FromResult(new GroupChatManagerResult<bool>(false)
        {
            Reason = "Awaiting approval"
        });
    }
}
