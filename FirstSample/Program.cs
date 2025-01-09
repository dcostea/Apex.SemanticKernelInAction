﻿using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var kernel = Kernel.CreateBuilder()
    //.AddAzureOpenAIChatCompletion(
    //deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    //endpoint: configuration["AzureOpenAI:Endpoint"]!,
    //apiKey: configuration["AzureOpenAI:ApiKey"]!)
    .AddOpenAIChatCompletion(
            modelId: configuration["OpenAI:ModelId"]!,
            apiKey: configuration["OpenAI:ApiKey"]!)
    .Build();

var response = await kernel.InvokePromptAsync("""
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    Your task is to break down complex commands into a sequence of these basic moves.
                    
    ## Complex command:
    "There is a tree directly in front of the car. Avoid it and then resume the initial direction."
    """);

Console.WriteLine(response);

/*
To avoid the tree directly in front of the car and then resume the initial direction, we can break down the complex command into a sequence of basic moves. Here's one way to execute the command:

1. **Stop:** Ensure the car is not moving before attempting to avoid the tree.
2. **Turn Right:** Begin turning to the right to steer away from the tree.
3. **Forward:** Move forward to pass around the tree.
4. **Turn Left:** Adjust the angle to begin aligning back toward the initial direction once you are past the tree.
5. **Forward:** Continue moving forward to clear the tree completely.
6. **Turn Left:** Turn left again to fully return to the original path.
7. **Forward:** Resume moving in the initial direction.

This sequence of moves should allow the car to successfully avoid the tree and continue on its intended path. 
 */