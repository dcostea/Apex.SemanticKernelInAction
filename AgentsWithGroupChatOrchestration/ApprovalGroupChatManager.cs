using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentsWithGroupChatOrchestration;

public sealed class ApprovalGroupChatManager : RoundRobinGroupChatManager
{
    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
        ChatHistory history, CancellationToken cancellationToken = default)
    {
        bool isApproved = history.LastOrDefault()?.Content?.Contains("APPROVED", StringComparison.InvariantCultureIgnoreCase) == true
            && history.LastOrDefault()?.Content?.Contains("NOT APPROVED", StringComparison.InvariantCultureIgnoreCase) == false;

        // Approval termination
        if (isApproved)
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
