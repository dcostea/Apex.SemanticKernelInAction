using Microsoft.SemanticKernel;
using System.ComponentModel;
using Plugins.Enums;

namespace Plugins.Native;

[Description("Rain detection and response plugin.")]
public class RainDetectorPlugin
{
    private const int Delay = 2000; // 2 seconds delay for mocking an action

    [KernelFunction("start_wipers"), Description("Start wipers for the droplet level detected by the sensor (e.g., light drizzle vs heavy rain).")]
    public async Task<string> StartWipers(DropletLevel dropletLevel)
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Starting wipers for droplet level: {dropletLevel}.");
        await Task.Delay(Delay);
        return await Task.FromResult($"Wipers have started for droplet level: {dropletLevel}.");
    }

    [KernelFunction("stop_wipers"), Description("Turn off wipers once no droplets are detected for a predefined duration (e.g., 10 seconds).")]
    public async Task<string> StopWipers()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Stopping wipers.");
        await Task.Delay(Delay);
        return await Task.FromResult("Wipers have stopped.");
    }
}

