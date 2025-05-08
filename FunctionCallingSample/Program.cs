using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Plugins.Native;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// Load configuration from user secrets (e.g., API keys, endpoints)
var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

// Create a kernel builder to configure the Semantic Kernel
var builder = Kernel.CreateBuilder();

// Add Azure OpenAI as the chat completion provider
builder.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!, // Azure OpenAI deployment name
    endpoint: configuration["AzureOpenAI:Endpoint"]!, // Azure OpenAI endpoint
    apiKey: configuration["AzureOpenAI:ApiKey"]! // Azure OpenAI API key
);
//builder.AddOpenAIChatCompletion(
//    modelId: configuration["OpenAI:ModelId"]!, // OpenAI model ID
//    apiKey: configuration["OpenAI:ApiKey"]! // OpenAI API key
//);
// builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

// Import the MotorsPlugin, which contains functions for robot control
kernel.ImportPluginFromType<MotorsPlugin>();

// Retrieve specific functions from the MotorsPlugin
var forward = kernel.Plugins.GetFunction("MotorsPlugin", "forward"); // Function to turn left
var backward = kernel.Plugins.GetFunction("MotorsPlugin", "backward"); // Function to turn right

// Create a subset of functions (only turn_left and turn_right) for potential use
KernelFunction[] subset = [forward, backward];

// Configure the function choice behavior for the AI
var executionSettings = new OpenAIPromptExecutionSettings
{
    // FunctionChoiceBehavior determines how the AI interacts with functions:
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), // AI can choose to call functions or respond with text
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Auto([]), // AI can choose, but no functions are available
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(subset), // AI can choose, but only from the subset
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Required(), // AI must call a function
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Required([]), // AI must call a function, but none are available
    // FunctionChoiceBehavior = FunctionChoiceBehavior.Required(subset), // AI must call a function from the subset
    // FunctionChoiceBehavior = FunctionChoiceBehavior.None(), // Function calling is disabled
    // FunctionChoiceBehavior = FunctionChoiceBehavior.None([]), // Function calling is disabled, no functions available
    // FunctionChoiceBehavior = FunctionChoiceBehavior.None(subset), // Function calling is disabled, subset ignored
    // FunctionChoiceBehavior = null, // No specific behavior is defined
};

// Create a chat history object to store the conversation context
var history = new ChatHistory();

// Add a system message to define the AI's role and permitted actions
history.AddSystemMessage("""
    You are an AI assistant controlling a robot car.
    The available robot car permitted moves are forward, backward, turn left, turn right, and stop.
    """
);

// Add a user message with specific instructions for the robot car
history.AddUserMessage("""
    Perform these steps:
      {{forward 100}}
      {{backward 10}}
      {{stop}} 
      {{forward 200}}
      {{backward 20}}
      {{stop}} 
      {{forward 300}}
      {{backward 30}}
      {{stop}} 
    
    Respond only with the moves, without any additional explanations.
    """
);

//Respond only with the moves, without any additional explanations.
//Respond with what tools would you call to perform the previous steps?

// Retrieve the chat completion service from the kernel
var chat = kernel.GetRequiredService<IChatCompletionService>();

// Generate a response from the AI based on the chat history and execution settings
var response = await chat.GetChatMessageContentAsync(history, executionSettings, kernel);

// Output the AI's response to the console
Console.WriteLine($"RESPONSE: {response}");

// Print the chat history for debugging or review
Helpers.Printing.PrintTools(history);
