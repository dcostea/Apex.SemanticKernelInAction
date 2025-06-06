using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Plugins.Native;

//const string ModelPath = @"c:\Users\dcost\.aitk\models\DeepSeek\deepseek-r1-distill-qwen-7b-cuda-int4-awq-block-128-acc-level-4";
const string ModelPath = @"c:\Users\dcost\.foundry\cache\models\Microsoft\Phi-4-cuda-gpu\cuda-int4-rtn-block-32";
//const string ModelPath = @"c:\Users\dcost\.aitk\models\microsoft\Phi-4-mini-instruct-onnx\gpu\gpu-int4-rtn-block-32";
//const string ModelPath = @"c:\Users\dcost\.aitk\models\microsoft\Phi-4-mini-instruct-onnx\cpu_and_mobile\cpu-int4-rtn-block-32-acc-level-4";
//const string ModelPath = @"c:\Temp\LLMs\ONNX\phi-4-multimodal-instruct-onnx\gpu\gpu-int4-rtn-block-32";
//const string ModelPath = @"c:\Users\dcost\.foundry\cache\models\Microsoft\mistralai-Mistral-7B-Instruct-v0-2-cuda-gpu\mistral-7b-instruct-v0.2-cuda-int4-rtn-block-32";

var builder = Kernel.CreateBuilder();
builder.AddOnnxRuntimeGenAIChatCompletion(
    modelPath: ModelPath,
    modelId: "onnx");
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var motorPlugin = kernel.ImportPluginFromType<MotorsPlugin>();

ChatHistory chatHistory = new("""
    You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    """);
chatHistory.AddUserMessage("""
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the permitted moves, without any additional explanations.

    But first, run this command (always turn in angles of 90 degrees):
    {{turn_right $angle}}

    Complex command:
    "There is a tree directly in front of the car. Avoid it and then come back to the original path."

    Then call the available tools / functions for the found basic moves
    """);

var prompt = """
    <message role="system">
      You are an AI assistant controlling a robot car capable of performing basic moves: forward, backward, turn left, turn right, and stop.
    </message>
    <message role="user">
      You have to break down the provided complex commands into basic moves you know.
      Respond only with the permitted moves, without any additional explanations.
    
      But first, run this basic command (always turn in angles of 90 degrees):
      {{turn_right $angle}}
    
      Complex command:
      "There is a tree directly in front of the car. Avoid it and then come back to the original path."
      
      Then call the available tools / functions for the found basic moves
    </message>
    """;

//{{forward}}

OnnxRuntimeGenAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0.1F,
    TopP = 0.95F,
    MaxTokens = 300,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Required(motorPlugin),
};

var kernelArguments = new KernelArguments(executionSettings)
{
    ["angle"] = "90",
};

var result = await kernel.InvokePromptAsync(prompt, kernelArguments);
Console.WriteLine($"RESPONSE: {result}");

var chat = kernel.GetRequiredService<IChatCompletionService>();
var response = await chat.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
Console.WriteLine($"RESPONSE: {response}");

//await foreach (var chatUpdate in chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
//{
//    Console.Write(chatUpdate.Content);
//}
//Console.WriteLine();

foreach (var target in kernel.GetAllServices<IChatCompletionService>().OfType<IDisposable>())
{
    // Update the method call to include the required parameters for AddOnnxRuntimeGenAIChatCompletion.
    // Based on the context, the method likely requires additional arguments such as modelId and other configuration options.
    target.Dispose();
}

//var response = await chat.GetChatMessageContentAsync(chatHistory);
//Console.WriteLine(response);
