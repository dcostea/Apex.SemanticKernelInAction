using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins.Native;

[Description("Robot car motors plugin.")]
public class MotorsPlugin
{
    private const int Delay = 3000; // 2 seconds delay for mocking an action

    [KernelFunction("backward"), Description("Basic command: Moves the robot car backward.")]
    public async Task<string> Backward([Description("The distance (in meters) to move the robot car backward.")] int? distance = null)
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Backward: {distance}m");
        await Task.Delay(Delay);
        return await Task.FromResult($"moved backward for {(distance == null ? "a few " : distance)} meters.");
    }

    [KernelFunction("forward"), Description("Basic command: Moves the robot car forward.")]
    public async Task<string> Forward([Description("The distance (in meters) to move the robot car forward.")] int? distance = null)
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Forward: {distance}m");
        await Task.Delay(Delay);
        return await Task.FromResult($"moved forward for {(distance == null ? "a few " : distance)} meters.");
    }

    [KernelFunction("stop"), Description("Basic command: Stops the robot car.")]
    public async Task<string> Stop()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Stop");
        await Task.Delay(Delay);
        return await Task.FromResult("stopped.");
    }

    [KernelFunction("turn_left"), Description("Basic command: Turns the robot car anticlockwise.")]
    public async Task<string> TurnLeft([Description("The angle (in ° / degrees) to turn the robot car anticlockwise.")] int angle)
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: TurnLeft: {angle}°");
        await Task.Delay(Delay);
        return await Task.FromResult($"turned anticlockwise {angle}°.");
    }

    [KernelFunction("turn_right"), Description("Basic command: Turns the robot car clockwise.")]
    public async Task<string> TurnRight([Description("The angle (in ° / degrees) to turn the robot car clockwise.")] int angle)
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: TurnRight: {angle}°");
        await Task.Delay(Delay);
        return await Task.FromResult($"turned clockwise {angle}°.");
    }
}
