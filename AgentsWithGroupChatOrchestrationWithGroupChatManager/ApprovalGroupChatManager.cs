using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentsWithGroupChatOrchestration;

public class ApprovalGroupChatManager(OrchestrationMonitor monitor) : RoundRobinGroupChatManager
{
    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
        ChatHistory history, CancellationToken cancellationToken = default)
    {
        // Extract approval state from the last message, except for messages from "MotorsAgent"
        ////var lastNonMotorsAgentMessage = history.LastOrDefault(h => h.AuthorName != "MotorsAgent")?.Content;
        ////bool isApproved = lastNonMotorsAgentMessage?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) ?? false;

        // Approval termination
        if (monitor.IsApproved)
        {
            var terminationMessage = $"Termination: [APPROVED]";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(terminationMessage);
            Console.ResetColor();

            return await ValueTask.FromResult(new GroupChatManagerResult<bool>(true)
            {
                Reason = terminationMessage
            });
        }

        // Maximum invocation count termination (first invoke the base method to update the invocation counter)
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
