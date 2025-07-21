using AgentsWithConcurrentOrchestration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgentsWithGroupChatOrchestration;

public class MonitoredApprovalGroupChatManager : RoundRobinGroupChatManager
{
    private readonly OrchestrationMonitor _monitor;

    public MonitoredApprovalGroupChatManager(OrchestrationMonitor monitor)
    {
        _monitor = monitor;
    }

    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
        ChatHistory history, CancellationToken cancellationToken = default)
    {
        string approvalState = _monitor.IsApproved 
            ? "[APPROVED]" 
            : "[DENIED]";
        string stateMessage = $"State[{InvocationCount}]: {approvalState}";
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(stateMessage);
        Console.ResetColor();

        // Approval termination
        if (_monitor.IsApproved && history.LastOrDefault()?.AuthorName == "MotorsAgent")
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
