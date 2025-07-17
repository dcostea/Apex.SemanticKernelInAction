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

    //public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default)
    //{
    //    string? lastAgent = history.LastOrDefault()?.AuthorName;

    //    if (lastAgent is null)
    //    {
    //        return ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "No agents have spoken yet." });
    //    }

    //    if (lastAgent == "MotorsAgent")
    //    {
    //        return ValueTask.FromResult(new GroupChatManagerResult<bool>(true) { Reason = "User input is needed after the MotorsAgent's message." });
    //    }

    //    return ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "User input is not needed until the MotorsAgent's message." });
    //}

    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
        ChatHistory history, CancellationToken cancellationToken = default)
    {
        string approvalState = _monitor.Approved ? "[APPROVED]" : "[NOT APPROVED]";
        string executionState = _monitor.Executed ? "[EXECUTED]" : "[NOT EXECUTED]";
        string stateMessage = $"State[{InvocationCount}]: {approvalState} {executionState}";
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(stateMessage);
        Console.ResetColor();

        // Approval termination
        if (_monitor.Approved && _monitor.Executed)
        {
            var terminationMessage = $"Termination: {approvalState} and {executionState}";

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
