using Helpers;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

public class TransientPlugin
{
    public static string? FireSafetyReport { get; set; }
    public static string? RainSafetyReport { get; set; }
    public static string? EnvironmentReport { get; set; }

    [KernelFunction("load_fire_safety_report")]
    [Description("Load fire safety report")]
    public string LoadFireSafetyReport()
    {
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Loading fire safety report: {FireSafetyReport ?? "no fire safety report"}");
        return FireSafetyReport is null
            ? "'fire safety report': no report"
            : $"'fire safety report': {FireSafetyReport}";
    }

    [KernelFunction("save_fire_safety_report")]
    [Description("Saving fire safety report")]
    public void SaveFireSafetyReport([Description("The fire safety report to be saved")] string fireSafetyReport)
    {
        FireSafetyReport = fireSafetyReport;
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Saving fire safety report {FireSafetyReport}");
    }

    [KernelFunction("load_rain_safety_report")]
    [Description("Load rain safety report")]
    public string LoadRainSafetyReport()
    {
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Loading rain safety report: {RainSafetyReport ?? "no rain safety report"}");
        return RainSafetyReport is null
            ? "'rain safety report': no report"
            : $"'rain safety report': {RainSafetyReport}";
    }

    [KernelFunction("save_rain_safety_report")]
    [Description("Saving rain safety report")]
    public void SaveRainSafetyReport([Description("The rain safety report to be saved")] string rainSafetyReport)
    {
        RainSafetyReport = rainSafetyReport;
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Saving rain safety report {RainSafetyReport}");
    }

    [KernelFunction("load_environment_report")]
    [Description("Load environment report")]
    public string LoadEnvironmentReport()
    {
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Loading environment report: {EnvironmentReport ?? "no environment report"}");
        return EnvironmentReport is null
            ? "'environment report': no report"
            : $"'environment report': {EnvironmentReport}";
    }

    [KernelFunction("save_environment_report")]
    [Description("Saving environment report")]
    public void SaveEnvironmentReport([Description("The environment report to be saved")] string environmentReport)
    {
        EnvironmentReport = environmentReport;
        ColoredConsole.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] TRANSIENT: Saving environment report {EnvironmentReport}");
    }
}
