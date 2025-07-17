﻿using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Plugins.Native;

[Description("Maintenance plugin for robot car.")]
public class MaintenancePlugin
{
    private const int Delay = 500; // x seconds delay for mocking an action

    [KernelFunction("calibrate_sensors"), Description("Calibrates all sensors on the robot car.")]
    public async Task<string> CalibrateSensors()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] MAINTENANCE: CALIBRATING sensors...");
        await Task.Delay(Delay);
        return await Task.FromResult("All sensors have been calibrated.");
    }

    [KernelFunction("check_motors"), Description("Checks the motors of the robot car.")]
    public async Task<string> CheckMotors()
    {
        var random = new Random();
        var motorStatus = random.Next(0, 100);
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] MAINTENANCE: CHECKING motors. Status: {motorStatus}%");
        await Task.Delay(Delay);
        return await Task.FromResult($"Motors checked. Status: {motorStatus}% efficiency.");
    }

    [KernelFunction("check_tire_pressure"), Description("Checks the tire pressure of the robot car.")]
    public async Task<string> CheckTirePressure()
    {
        var random = new Random();
        var pressure = random.Next(30, 35);
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] MAINTENANCE: CHECKING tire pressure: {pressure} PSI");
        await Task.Delay(Delay);
        return await Task.FromResult($"Tire pressure is {pressure} PSI.");
    }

    [KernelFunction("clean_solar_panels"), Description("Cleans the solar panels of the robot car.")]
    public async Task<string> CleanSolarPanels()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] MAINTENANCE: CLEANING solar panels...");
        await Task.Delay(Delay * 3);
        return await Task.FromResult("Solar panels have been cleaned.");
    }

    [KernelFunction("check_battery_health"), Description("Checks the battery health of the robot car.")]
    public async Task<string> CheckBatteryHealth()
    {
        var random = new Random();
        var batteryHealth = random.Next(0, 100);
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] MAINTENANCE: CHECKING battery health: {batteryHealth}%");
        await Task.Delay(Delay);
        return await Task.FromResult($"Battery health is at {batteryHealth}%.");
    }

    [KernelFunction("update_firmware"), Description("Updates the firmware of the robot car.")]
    public async Task<string> UpdateFirmware()
    {
        Console.WriteLine($"[{DateTime.Now:hh:mm:ss:fff}] MAINTENANCE: UPDATING firmware...");
        await Task.Delay(Delay * 5);
        return await Task.FromResult("Firmware has been updated to the latest version.");
    }
}
