using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

[Description("Robot car motors plugin.")]
public class MotorsPlugin
{
    [KernelFunction("backward"), Description("Basic command: Moves the robot car backward.")]
    public async Task<string> Backward([Description("The distance (in meters) to move the robot car.")] int? distance = null)
    {
        Console.WriteLine($"[{DateTime.Now:mm:ss}] ACTION: Backward: {distance}m");
        await Task.Delay(2000);
        return await Task.FromResult($"moved backward for {(distance == null ? "a few " : distance)} meters.");
    }

    [KernelFunction("forward"), Description("Basic command: Moves the robot car forward.")]
    public async Task<string> Forward([Description("The distance (in meters) to move the robot car.")] int? distance = null)
    {
        Console.WriteLine($"[{DateTime.Now:mm:ss}] ACTION: Forward: {distance}m");
        await Task.Delay(2000);
        return await Task.FromResult($"moved forward for {(distance == null ? "a few " : distance)} meters.");
    }

    [KernelFunction("stop"), Description("Basic command: Stops the robot car.")]
    public async Task<string> Stop()
    {
        Console.WriteLine($"[{DateTime.Now:mm:ss}] ACTION: Stop");
        await Task.Delay(2000);
        return await Task.FromResult("stopped.");
    }

    [KernelFunction("turn_left"), Description("Basic command: Turns the robot car anticlockwise.")]
    public async Task<string> TurnLeft([Description("The angle (in ° / degrees) to turn the robot car.")] int angle)
    {
        Console.WriteLine($"[{DateTime.Now:mm:ss}] ACTION: TurnLeft: {angle}°");
        await Task.Delay(2000);
        return await Task.FromResult($"turned anticlockwise {angle}°.");
    }

    [KernelFunction("turn_right"), Description("Basic command: Turns the robot car clockwise.")]
    public async Task<string> TurnRight([Description("The angle (in ° / degrees) to turn the robot car.")] int angle)
    {
        Console.WriteLine($"[{DateTime.Now:mm:ss}] ACTION: TurnRight: {angle}°");
        await Task.Delay(2000);
        return await Task.FromResult($"turned clockwise {angle}°.");
    }
}
