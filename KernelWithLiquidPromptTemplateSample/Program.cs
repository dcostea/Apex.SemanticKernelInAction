using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
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

//declare the liquid prompt template
var template = """
    <message role="system">
        You are an AI assistant controlling a robot car capable of performing basic moves: {{robot_car.movements}}.
        You have to break down the provided complex commands into basic moves you know.
        Respond only with the moves, without any additional explanations.
        
        # Context
        robot car name: {{robot_car.name}}
    </message>
    {% for item in history %}
    <message role="{{item.role}}">
        {{item.content}}
    </message>
    {% endfor %}
    """;

var promptTemplateConfig = new PromptTemplateConfig()
{
    Template = template,
    TemplateFormat = "liquid",
    Name = "LiquidPrompt",
    Description = "Liquid prompt template for a robot car assistant"
};

var promptTemplateFactory = new LiquidPromptTemplateFactory();
var promptTemplate = promptTemplateFactory.Create(promptTemplateConfig);

// rendering prompt using prompt template and kernel arguments
var renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
//////await kernel.InvokePromptAsync(renderedPrompt, kernelArguments);

// querying using rendered prompt
var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
var renderedPromptResponse = await chatCompletion.GetChatMessageContentAsync(renderedPrompt);

// build the kernel function from propmt template config using the prompt template factory
var promptFunction = kernel.CreateFunctionFromPrompt(promptTemplateConfig, promptTemplateFactory);

// querying using prompt function and kernel arguments
var promptFunctionResponse = await kernel.InvokeAsync(promptFunction, kernelArguments);

// querying using using prompt invocation
////var promptTemplateResponse = await kernel.InvokePromptAsync(template, kernelArguments, "liquid", promptTemplateFactory);
