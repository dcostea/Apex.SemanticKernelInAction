using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Plugins;

public class TransientPlugin
{
    public int? CurrentNumber { get; set; }
    public int? StopCondition { get; set; }

    [KernelFunction("increment_number")]
    [Description("Incrementing number")]
    public string Increment([Description("Current number to increment")] [Required] int currentNumber, [Description("Amount to increment with")] int amount)
    {
        return $"The incremented number is {currentNumber + amount}";
    }

    [KernelFunction("load_current_number")]
    [Description("Load the current number (output type: int)")]
    public int LoadCurrentNumber()
    {
        return CurrentNumber ?? 0;
    }

    [KernelFunction("save_current_number")]
    [Description("Save current number")]
    public void SaveCurrentNumber([Description("The current number to be saved")] [Required] int currentNumber)
    {
        CurrentNumber = currentNumber;
    }

    [KernelFunction("load_stop_condition")]
    [Description("Load stop condition (output type: int)")]
    public int LoadStopCondition()
    {
        return StopCondition ?? 0;
    }

    [KernelFunction("save_stop_condition")]
    [Description("Save stop condition")]
    public void SaveStopCondition([Description("The stop condition to be saved")] [Required] int stopCondition)
    {
        StopCondition = stopCondition;
    }
}
