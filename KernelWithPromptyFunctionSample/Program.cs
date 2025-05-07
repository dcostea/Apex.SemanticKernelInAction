using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
using Microsoft.SemanticKernel.Prompty;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
//builder.AddAzureOpenAIChatCompletion(
//    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
//    endpoint: configuration["AzureOpenAI:Endpoint"]!,
//    apiKey: configuration["AzureOpenAI:ApiKey"]!);
builder.AddOpenAIChatCompletion(
    modelId: configuration["OpenAI:ModelId"]!,
    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = builder.Build();

var chatMessages = new[]
{
    new { role = "user", content = "Go round." },
    new { role = "assistant", content = "left, left, left, left" },
    new { role = "user", content = "Go on a square." },
    new { role = "assistant", content = "left, forward, left, forward, left, forward, left, forward" },
    new { role = "user", content = "Repeat again the first user action." },
};

// Prompty template is fully packed with all settings and parameters, along with system and user prompts.
var promptyTemplate = """
    ---
    name: RobotCarPrompt
    description: An AI assistant controlling a robot car.
    authors:
      - Daniel
    model:
      api: chat
      configuration:
        type: openai
      parameters:
        max_tokens: 500
        temperature: 0.1
    ---
    system:

    You are an AI assistant controlling a robot car capable of performing basic moves: {{robot_car.movements}}.
    You have to break down the provided complex commands into basic moves you know.
    Respond only with the moves, without any additional explanations.
    
    robot car name: {{robot_car.name}}

    {% for item in history %}
    {{item.role}}:
    {{item.content}}
    {% endfor %}

    user:
    There is a tree directly in front of the car. Avoid it and then come back to the original path.
    """;

var robotCar = new
{
    name = "Robby",
    movements = "forward, backward, turn left, turn right, and stop"
};

var kernelArguments = new KernelArguments
{
    ["robot_car"] = robotCar,
    ["history"] = chatMessages
};

#pragma warning disable SKEXP0040 // CreateFunctionFromPrompty is experimental and it needs to be enabled explicitly

var promptyFunction = kernel.CreateFunctionFromPrompty(promptyTemplate);
// querying using using prompt function invocation and kernel arguments
var result = await kernel.InvokeAsync(promptyFunction, kernelArguments);
//Console.WriteLine(result);

//var promptTemplateFactory = new LiquidPromptTemplateFactory();
//// extract the prompt template config from the prompty template
//var promptTemplateConfig = KernelFunctionPrompty.ToPromptTemplateConfig(promptyTemplate);
//var promptTemplate = promptTemplateFactory.Create(promptTemplateConfig);
//// rendering prompt using prompt template config and template factory
//var renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
//Console.WriteLine(renderedPrompt);

//// querying using rendered prompt
//var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
//var renderedPromptResponse = await chatCompletion.GetChatMessageContentAsync(renderedPrompt);
