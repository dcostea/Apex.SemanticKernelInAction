using OllamaSharp;

namespace Tools;

public class MotorTools
{
    /// <summary>
    /// Basic command: Moves the robot car backward.
    /// </summary>
    [OllamaTool]
    public static void Backward()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Backward");
    }

    /// <summary>
    /// Basic command: Moves the robot car forward.
    /// </summary>
    [OllamaTool]
    public static void Forward()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Forward");
    }

    /// <summary>
    /// Basic command: Stops the robot car.
    /// </summary>
    [OllamaTool]
    public static void Stop()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: Stop");
    }

    /// <summary>
    /// Basic command: Turns the robot car anticlockwise.
    /// </summary>
    [OllamaTool]
    public static void TurnLeft()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: TurnLeft");
    }

    /// <summary>
    /// Basic command: Turns the robot car clockwise.
    /// </summary>
    [OllamaTool]
    public static void TurnRight()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] ACTION: TurnRight°");
    }
}
