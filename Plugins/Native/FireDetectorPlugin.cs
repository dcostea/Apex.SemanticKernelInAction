using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins.Native;

[Description("Fire detection and response plugin.")]
public class FireDetectorPlugin
{
    private const int Delay = 2000; // 2 seconds delay for mocking an action

    [KernelFunction("capture_camera_feed"), Description("Use computer vision to visually confirm fire presence and size.")]
    public async Task<string> CaptureCameraFeed()
    {
        // Simulate capturing camera feed and converting it to text
        var random = new Random();
        var isFire = random.Next(0, 4); // Simulate true or false
        var feed = isFire == 0
            ? "No fire detected in the camera feed."
            : "Fire detected in the camera feed!";
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: CAMERA FEED {feed}");
        await Task.Delay(Delay);
        return await Task.FromResult(feed);
    }

    [KernelFunction("sound_alarm"), Description("Trigger an audible or visual alarm to warn nearby humans or systems.")]
    public async Task SoundAlarm()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Sounding fire alarm!");
        await Task.Delay(Delay);
    }

    [KernelFunction("start_water_sprinkle"), Description("Activate sprinklers to suppress the fire.")]
    public async Task StartWaterSprinkle()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Starting water sprinkle.");
        await Task.Delay(Delay);
    }

    [KernelFunction("stop_water_sprinkle"), Description("Stop sprinklers when fire is extinguished.")]
    public async Task StopWaterSprinkle()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Stopping water sprinkle.");
        await Task.Delay(Delay);
    }
}
