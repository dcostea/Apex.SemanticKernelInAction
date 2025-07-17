using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

public class TransientPlugin
{
    public int? CurrentNumber { get; set; } = null;

    [KernelFunction("increment_number")]
    [Description("Increment the current number")]
    public string Increment([Description("Current number to be incremented")] int currentNumber, int incrementAmount)
    {
        return $"The incremented number is {currentNumber + incrementAmount}";
    }

    [KernelFunction("decrement_number")]
    [Description("Decrement the current number")]
    public string Decrement([Description("Current number to be decremented")] int currentNumber, int decrementAmount)
    {
        return $"The decremented number is {currentNumber - decrementAmount}";
    }

    [KernelFunction("multiply_number")]
    [Description("Multiply the current number")]
    public string Multiplying([Description("Current number to be multiplied")] int currentNumber, [Description("The multiplier")] int multiplier)
    {
        return $"The multiplied number is {currentNumber * multiplier}";
    }

    [KernelFunction("load_current_number")]
    [Description("Load the current number")]
    public int LoadCurrentNumber()
    {
        return CurrentNumber ?? 0;
    }

    [KernelFunction("save_current_number")]
    [Description("Save the current number")]
    public void SaveCurrentNumber([Description("Current number to be saved")] int currentNumber)
    {
        CurrentNumber = currentNumber;
    }
}