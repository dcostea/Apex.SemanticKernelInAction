using System.ComponentModel;

namespace Models;

internal class SafetyClearance
{
    [Description("The rain status reported by the Rain Safety Agent. Respond only with the status, no reasoning, no comments.")]
    public string RainStatus { get; init; } = string.Empty;

    [Description("The fire status reported by the Fire Safety Agent. Respond only with the status, no reasoning, no comments..")]
    public string FireStatus { get; init; } = string.Empty;

    [Description("The reason for the safety clearance decision.")]
    public string Reason { get; init; } = string.Empty;
}
