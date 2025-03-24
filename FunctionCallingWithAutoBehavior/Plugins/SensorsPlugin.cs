using Microsoft.SemanticKernel;
using Plugins.Enums;
using System.ComponentModel;

namespace Plugins;

[Description("Robot car sensors plugin.")]
public class SensorsPlugin
{
    [KernelFunction("read_temperature"), Description("Use thermal sensors to detect abnormal heat levels.")]
    public async Task<int> ReadTemperature()
    {
        var random = new Random();
        var temperature = random.Next(-20, 100); // Simulate temperature reading
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Temperature: {temperature} Celsius degrees.");
        await Task.Delay(3000);
        return await Task.FromResult(temperature);
    }

    [KernelFunction("read_infrared_radiation"), Description("Confirm the presence of flames via IR sensors.")]
    public async Task<int> ReadInfraredRadiation()
    {
        var random = new Random();
        var irLevel = random.Next(0, 100); // Simulate IR reading
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Infrared Radiation: {irLevel}");
        await Task.Delay(3000);
        return await Task.FromResult(irLevel);
    }

    [KernelFunction("read_humidity"), Description("Check local humidity as a precursor to rain detection (fires often reduce local moisture).")]
    public async Task<int> ReadHumidity()
    {
        var random = new Random();
        var humidity = random.Next(0, 100); // Simulate humidity reading
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Humidity: {humidity} %");
        await Task.Delay(3000);
        return await Task.FromResult(humidity);
    }

    [KernelFunction("read_distance_to_object"), Description("Use ultrasonic sensors to measure the distance from the fire and ensure safe retreat.")]
    public async Task<int> ReadDistanceToObject()
    {
        var random = new Random();
        var distance = random.Next(0, 500); // Simulate distance reading in cm
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Distance to Object: {distance} cm");
        await Task.Delay(3000);
        return await Task.FromResult(distance);
    }

    [KernelFunction("read_droplet_level"), Description("Use optical or capacitive rain sensors to measure the presence and intensity of raindrops on surfaces like windshields or body panels.")]
    public async Task<DropletLevel> ReadDropletLevel()
    {
        var random = new Random();
        var values = Enum.GetValues(typeof(DropletLevel));
        var dropletLevel = (DropletLevel)values.GetValue(random.Next(values.Length))!; // Simulate droplet level reading
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Droplet Level: {dropletLevel}");
        await Task.Delay(3000);
        return await Task.FromResult(dropletLevel);
    }

    [KernelFunction("read_wind_speed"), Description("Reads and returns the wind speed in kmph.")]
    public async Task<int> ReadWindSpeed()
    {
        var random = new Random();
        var speed = random.Next(0, 100);
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Wind speed: {speed} kmph"); // Simulate wind speed reading
        await Task.Delay(3000);
        return await Task.FromResult(speed);
    }

    [KernelFunction("read_wind_direction"), Description("Reads and returns the wind direction. The wind direction (output) is like North, NorthWest, etc.")]
    public async Task<Direction> ReadWindDirection()
    {
        var random = new Random();
        var values = Enum.GetValues(typeof(Direction));
        var direction = (Direction)values.GetValue(random.Next(values.Length))!;
        Console.WriteLine($"[{DateTime.Now:mm:ss}] SENSOR READING: Wind direction: {direction}"); // Simulate wind direction reading
        await Task.Delay(2000);
        return await Task.FromResult(direction);
    }
}
