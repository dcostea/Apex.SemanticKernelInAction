using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins;

[Description("Robot car plugin.")]
public class RobotCarPlugin
{
    [KernelFunction, Description("Basic command: Moves the robot car backward.")]
    public static async Task<string> Backward([Description("The distance (in meters) to move the robot car.")] int distance)
    {
        // TODO call car motor API, backward endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Backward: {distance}m");
        Console.ResetColor();

        return await Task.FromResult($"moving backward for {distance} meters...");
    }

    [KernelFunction, Description("Basic command: Moves the robot car forward.")]
    public static async Task<string> Forward([Description("The distance (in meters) to move the robot car.")] int distance)
    {
        // TODO call car motor API, forward endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.WriteLine($"Forward: {distance}m");
        Console.ResetColor();

        return await Task.FromResult($"moving forward for {distance} meters...");
    }

    [KernelFunction, Description("Basic command: Stops the robot car.")]
    public static async Task<string> Stop()
    {
        // TODO call car motor API, stop endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Stop");
        Console.ResetColor();

        return await Task.FromResult("stopping...");
    }

    [KernelFunction, Description("Basic command: Turns the robot car anticlockwise.")]
    public static async Task<string> TurnLeft([Description("The angle (in ° / degrees) to turn the robot car.")] int angle)
    {
        // TODO call car motor API, turn left endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"TurnLeft: {angle}°");
        Console.ResetColor();

        return await Task.FromResult($"turning anticlockwise {angle}°...");
    }

    [KernelFunction, Description("Basic command: Turns the robot car clockwise.")]
    public static async Task<string> TurnRight([Description("The angle (in ° / degrees) to turn the robot car.")] int angle)
    {
        // TODO call car motor API, turn right endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"TurnRight: {angle}°");
        Console.ResetColor();

        return await Task.FromResult($"turning clockwise {angle}°...");
    }
}