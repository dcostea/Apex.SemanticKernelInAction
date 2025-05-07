using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
var kernel = builder.Build();

// Create a semantic function
var semanticFunctionPrompt = """
    Your task is to break down complex commands into a sequence of these basic moves.
    The permitted basic moves are: {{$basic_moves}}.
    
    [COMPLEX COMMAND START]
    {{$input}}
    [COMPLEX COMMAND END]

    Commands:
    """;
var semanticFunction = kernel.CreateFunctionFromPrompt(semanticFunctionPrompt, functionName: "BreakdownComplexCommands", description: "It breaks down the given complex command into a step-by-step sequence of basic moves.");

// Create a native function
var nativeFunction = kernel.CreateFunctionFromMethod(
    typeof(MaintenanceFunctions).GetMethod(nameof(MaintenanceFunctions.CalibrateSensors))!,
    new MaintenanceFunctions(),
    "CalibrateSensors",
    "Calibrates all sensors on the robot car."
);

// Import both functions into a plugin
List<KernelFunction> hybridFunctions = [ semanticFunction, nativeFunction ];
kernel.ImportPluginFromFunctions("robot_car_plugin", "Robot car plugin.", hybridFunctions);

Helpers.Printing.PrintPluginsWithFunctions(kernel);

var kernelArguments = new KernelArguments
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

// The prompt calls CalibrateSensors function from the kernel plugin
var prompt = """
    You are an AI assistant controlling a robot car.
    Status: {{CalibrateSensors}}

    Provide only the sequence of the basic movements, without any additional explanations.
        
    Complex command: {{$input}}.
    """;

// Invoke the prompt with the kernel arguments and kernel plugin
var response = await kernel.InvokePromptAsync(prompt, kernelArguments);
Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}");
Console.WriteLine($"RESPONSE: {response}");


public class MaintenanceFunctions
{
    public static async Task<string> CalibrateSensors()
    {
        Console.WriteLine($"[{DateTime.Now:mm:ss}] CALIBRATING sensors...");
        return await Task.FromResult("All sensors have been calibrated.");
    }
}
