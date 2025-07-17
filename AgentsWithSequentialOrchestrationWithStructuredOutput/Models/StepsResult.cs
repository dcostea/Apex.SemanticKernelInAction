using System.ComponentModel;

namespace Models;

public sealed class StepsResult
{
    [Description("The steps to be executed in movement sequence, each step is a string representing a basic command.")]
    public List<string>? Steps { get; set; }
}
