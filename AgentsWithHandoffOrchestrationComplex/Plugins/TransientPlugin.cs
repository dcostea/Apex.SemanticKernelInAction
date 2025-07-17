using Helpers;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

public class TransientPlugin
{
    public string? SafetyReport { get; set; } = null;

    public string? EnvironmentalReport { get; set; } = null;

    public string? EmergencyPlan { get; set; } = null;

    [KernelFunction("load_safety_report")]
    [Description("Load safety report")]
    public string LoadSafetyReport()
    {
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Loading safety report: {SafetyReport ?? "no safety report"}");
        return SafetyReport is null
            ? "'safety report': no report"
            : $"'safety report': {SafetyReport}";
    }

    [KernelFunction("save_safety_report")]
    [Description("Saving safety report")]
    public void SaveSafetyReport([Description("The safety report to be saved")] string safetyReport)
    {
        SafetyReport = safetyReport;
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Saving safety report {SafetyReport}");
    }

    [KernelFunction("load_environmental_report")]
    [Description("Load environment report")]
    public string LoadEnvironmentalReport()
    {
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Loading environmental report: {EnvironmentalReport ?? "no environmental report"}");
        return EnvironmentalReport is null
            ? "'environmental report': no report"
            : $"'environmental report': {EnvironmentalReport}";
    }

    [KernelFunction("save_environmental_report")]
    [Description("Save environmental report")]
    public void SaveEnvironmentalReport([Description("The environmental report to be saved")] string environmentalReport)
    {
        EnvironmentalReport = environmentalReport;
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Saving environmental report {EnvironmentalReport}");
    }

    [KernelFunction("load_emergency_plan")]
    [Description("Load emergency plan")]
    public string LoadEmergencyPlan()
    {
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Loading emergency plan: {EmergencyPlan ?? "no emergency plan"}");
        return EmergencyPlan is null
            ? "'emergency plan': no plan"
            : $"'emergency plan': {EmergencyPlan}";
    }

    [KernelFunction("save_emergency_plan")]
    [Description("Save emergency plan")]
    public void SaveEmergencyPlan([Description("The emergency plan to be saved")] string emergencyPlan)
    {
        EmergencyPlan = emergencyPlan;
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Saving emergency plan {EmergencyPlan}");
    }
}