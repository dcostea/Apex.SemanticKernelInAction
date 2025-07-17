using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

public class TransientPlugin
{
    public string? EnvironmentalReport { get; set; } = """
        **Environmental Report:**
        - Temperature: 28°C
        - Humidity: 42%
        - Rain Drops: High detected
        - Wind Speed: 77 km/h
        - Wind Direction: East
        """;
        
    [KernelFunction("load_environmental_report")]
    [Description("Load the environmental report")]
    public string LoadEnvironmentalReport()
    {
        return EnvironmentalReport ?? string.Empty;
    }
}