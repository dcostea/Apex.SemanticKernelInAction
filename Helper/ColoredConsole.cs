namespace Helpers;

public static class ColoredConsole
{
    private static readonly Lock _lock = new();


    public static void WriteLine(string line, ConsoleColor color)
    {
        lock (_lock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }

    public static void WriteLine(string line)
    {
        Write(line);
        Console.WriteLine();
    }

    public static void Write(string line)
    {
        lock (_lock)
        {
            if (line.Contains("HandoffPlugin-"))
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(line);
                Console.ResetColor();
                Console.Write("\n--------------------------------------------------");
            }
            else if (line.Contains("MotorsPlugin-"))
            {

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(line);
                Console.ResetColor();
            }
            else if (line.Contains("TransientPlugin-"))
            {

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(line);
                Console.ResetColor();
            }
            else if (line.Contains("Content:"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(line);
                Console.ResetColor();
            }
            else if (line.Contains("Plugin-"))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(line);
                Console.ResetColor();
            }
            else
            {
                Console.Write(line);
            }
        }
    }
}
