using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugins;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Diagnostics;
using OpenAI.Chat;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint"]!,
    apiKey: configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    modelId: configuration["OpenAI:ModelId"]!,
//    apiKey: configuration["OpenAI:ApiKey"]!);
//builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));
var kernel = builder.Build();

//kernel.ImportPluginFromType<RainDetectorPlugin>();
//kernel.ImportPluginFromType<FireDetectorPlugin>();
//kernel.ImportPluginFromType<SensorsPlugin>();
//kernel.ImportPluginFromType<MaintenancePlugin>();
kernel.ImportPluginFromType<MotorsPlugin>();

var chat = kernel.GetRequiredService<IChatCompletionService>();

var sw = new Stopwatch();

//var logger = kernel.Services.GetRequiredService<ILogger<Program>>();

// for sensors+ it is better to allow parallel calls
// for robot car it is better to allow concurrent invocation
// for maintenance it is better to allow concurrent invocation
var behaviorOptions = new FunctionChoiceBehaviorOptions 
{
    AllowConcurrentInvocation = true, // allow multiple function calls at the same time
    AllowParallelCalls = true, // prefer parallel function calls over sequential (multiple functions in one request instead of a tool for each request)
    AllowStrictSchemaAdherence = true
};

//_ = kernel.Plugins.TryGetFunction(nameof(SensorsPlugin), "read_temperature", out var temperature);

//var chatResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
//    jsonSchemaFormatName: "basic_moves",
//    jsonSchema: BinaryData.FromString("""
//        {
//          "type": "object",
//          "properties": {
//            "steps": {
//              "type": "array",
//              "items": {
//                "type": "string"
//              }
//            }
//          },
//          "required": ["steps"],
//          "additionalProperties": false
//        }
//        """),
//    jsonSchemaIsStrict: true); // additionalProperties = false will not allow additional properties in the response

// uncomment to use a subset of functions for auto behavior
var turnLeftFunction = kernel.Plugins.GetFunction("MotorsPlugin", "turn_left");
var turnRightFunction = kernel.Plugins.GetFunction("MotorsPlugin", "turn_right");
var stopFunction = kernel.Plugins.GetFunction("MotorsPlugin", "stop");

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var executionSettings = new AzureOpenAIPromptExecutionSettings
{
    //ResponseFormat = chatResponseFormat, //typeof(object),
    ResponseFormat = typeof(StepsResult),
    //FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: behaviorOptions, autoInvoke: true, functions: [turnLeftFunction, turnRightFunction, stopFunction]), // all plugin functions are advertised to the model, auto may repeat the same function, good for motors, bad for sensors
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: behaviorOptions, autoInvoke: true), // all plugin functions are advertised to the model, auto may repeat the same function, good for motors, bad for sensors
    //FunctionChoiceBehavior = null // disable function calling
};
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

sw.Start();

var history = new ChatHistory();
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    You are also equipped with sensors.
    """
);

//history.AddUserMessage("""
//    Is it raining? Depending on that, activate/deactive the wipers.
//    Do you sense any fire? Take safety measures if you do and quickly move away!
//    But first, calibrate all sensors.
//    """);

//history.AddUserMessage("""
//    What is the temperature?
//    """);

//history.AddUserMessage("""
//    Perform full maintenance check for the robot car subsystems.
//    """);

history.AddUserMessage("""
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.

    There is a tree directly in front of the car. Avoid it and then come back to the original path.
    """);

//history.AddUserMessage("""
//    Read all the robot car sensors.
//    Is it safe for the robot car to move forward?
//    What about human safety?
//    """); 


// autoInvoke: false will not invoke the functions, only return the function calls, but not the results of the function calls
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

// uncomment to invoke the function calls manually when using autoInvoke: false (this is done automatically when autoInvoke: true)
//var functionCalls = FunctionCallContent.GetFunctionCalls(response).ToList();
//var functionCallResult = await functionCalls[0].InvokeAsync(kernel);
//Console.WriteLine($"FUNCTION CALL RESPONSE: {functionCallResult.Result}");

sw.Stop();
Console.WriteLine($"RESPONSE (total time: {sw.Elapsed.TotalSeconds} seconds): {response}");


public sealed class StepsResult
{
    public List<string>? Steps { get; set; }
}