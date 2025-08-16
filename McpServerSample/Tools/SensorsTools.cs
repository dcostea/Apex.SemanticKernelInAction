using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Tools;

[McpServerToolType]
public static class SensorsTools
{
    [McpServerTool(Name = "read_temperature"), Description("Use thermal sensors to detect abnormal heat levels.")]
    public static async Task<int> ReadTemperature()
    {
        var random = new Random();
        var temperature = random.Next(-20, 100); // Simulate temperature reading
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] SENSORS: READING Temperature: {temperature} Celsius degrees.");
        return await Task.FromResult(temperature);
    }
}
