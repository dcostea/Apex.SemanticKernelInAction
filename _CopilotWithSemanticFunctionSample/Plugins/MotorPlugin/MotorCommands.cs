using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace CopilotSampleWithSemanticFunction.Plugins.MotorPlugin;

[Description("Car motor plugin.")]
public class MotorCommands
{
    [KernelFunction, Description("Moves the car backward.")]
    public static async Task<string> Backward()
    {
        // TODO call car motor API, backward endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Backward");
        Console.ResetColor();

        return await Task.FromResult("moving backward...");
    }

    [KernelFunction, Description("Moves the car forward.")]
    public static async Task<string> Forward()
    {
        // TODO call car motor API, forward endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.WriteLine("Forward");
        Console.ResetColor();

        return await Task.FromResult("moving forward...");
    }

    [KernelFunction, Description("Stops the car.")]
    public static async Task<string> Stop()
    {
        // TODO call car motor API, stop endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Stop");
        Console.ResetColor();

        return await Task.FromResult("stopping...");
    }

    [KernelFunction, Description("Turns the car anticlockwise.")]
    public static async Task<string> TurnLeft()
    {
        // TODO call car motor API, turn left endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("TurnLeft");
        Console.ResetColor();

        return await Task.FromResult("turning anticlockwise...");
    }

    [KernelFunction, Description("Turns the car clockwise.")]
    public static async Task<string> TurnRight()
    {
        // TODO call car motor API, turn right endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("TurnRight");
        Console.ResetColor();

        return await Task.FromResult("turning clockwise...");
    }
}