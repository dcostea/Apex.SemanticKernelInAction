using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace McpServerWithHttp.Plugins;

[Description("Simulated robot car motors plugin.")]
public class MotorsPlugin
{
    private const int Delay = 100;

    [KernelFunction("backward"), Description("Basic command: Moves the robot car backward.")]
    public async Task<string> Backward(
        [Description("The distance (in meters) to move backward.")] int distance = 1,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, cancellationToken);
        return $"moved backward for {distance} meters.";
    }

    [KernelFunction("forward"), Description("Basic command: Moves the robot car forward.")]
    public async Task<string> Forward(
        [Description("The distance (in meters) to move forward.")] int distance = 1,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, cancellationToken);
        return $"moved forward for {distance} meters.";
    }

    [KernelFunction("stop"), Description("Basic command: Stops the robot car.")]
    public async Task<string> Stop(CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, cancellationToken);
        return "stopped.";
    }

    [KernelFunction("turn_left"), Description("Basic command: Turns the robot car anticlockwise.")]
    public async Task<string> TurnLeft(
        [Description("The angle (in degrees) to turn anticlockwise.")] int angle,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, cancellationToken);
        return $"turned anticlockwise {angle}°.";
    }

    [KernelFunction("turn_right"), Description("Basic command: Turns the robot car clockwise.")]
    public async Task<string> TurnRight(
        [Description("The angle (in degrees) to turn clockwise.")] int angle,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(Delay, cancellationToken);
        return $"turned clockwise {angle}°.";
    }
}
