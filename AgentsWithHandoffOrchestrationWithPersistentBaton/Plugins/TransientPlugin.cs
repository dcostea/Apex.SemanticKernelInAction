using Helpers;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

public class TransientPlugin
{
    public string? SafetyClearance { get; set; } = null;

    [KernelFunction("load_safety_clearance")]
    [Description("Load safety clearance")]
    public string LoadSafetyClearance()
    {
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Loading safety clearance: {SafetyClearance ?? "no safety clearance"}");
        return SafetyClearance is null
            ? "'safety clearance': no clearance"
            : $"'safety clearance': {SafetyClearance}";
    }

    [KernelFunction("save_safety_clearance")]
    [Description("Saving safety clearance")]
    public void SaveSafetyClearance([Description("The safety clearance to be saved")] string safetyClearance)
    {
        SafetyClearance = safetyClearance;
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Saving safety clearance {SafetyClearance}");
    }
}