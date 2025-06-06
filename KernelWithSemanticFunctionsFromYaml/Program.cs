using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
    endpoint: configuration["AzureOpenAI:Endpoint"]!,
    apiKey: configuration["AzureOpenAI:ApiKey"]!);
//builder.AddOpenAIChatCompletion(
//    modelId: configuration["OpenAI:ModelId"]!,
//    apiKey: configuration["OpenAI:ApiKey"]!);
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
var kernel = builder.Build();

var yamlPrompt = """
    name: BreakdownComplexCommands
    template: |
      Your task is to break down complex commands into a sequence of the basic moves such as {{$basic_moves}}.
      [COMPLEX COMMAND START]
      {{$input}}
      [COMPLEX COMMAND END]
      Provide only the sequence of the basic movements, without any additional explanations.
    template_format: semantic-kernel
    description: It breaks down the given complex command into a step-by-step sequence of basic moves.
    input_variables:
      - name: input
        description: The complex command to break down into a step-by-step sequence of basic moves.
        is_required: true
      - name: basic_moves
        description: The allowed basic moves.
        is_required: true
    execution_settings:
      default:
        temperature: 0.1
        max_tokens: 1000
    """;

// Preparing the prompt function from yaml text
var promptFunctionFromYaml = kernel.CreateFunctionFromPromptYaml(yamlPrompt);

Console.WriteLine($"""
    SEMANTIC FUNCTION:
      Name: {promptFunctionFromYaml.Name}
      Description: '{promptFunctionFromYaml.Description}'
      Plugin name: '{promptFunctionFromYaml.PluginName}'
      Execution settings: {string.Join(" ", promptFunctionFromYaml!.ExecutionSettings!["default"].ExtensionData!)}
      Input variable: {string.Join("", promptFunctionFromYaml.Metadata.Parameters.Select(p => $"\n    {p.Name} : {p.ParameterType!.Name} {(p.IsRequired ? "required" : "")} '{p.Description}'"))}
      Output variable: {promptFunctionFromYaml.Metadata.ReturnParameter.Schema} {promptFunctionFromYaml.Metadata.ReturnParameter.ParameterType} '{promptFunctionFromYaml.Metadata.ReturnParameter.Description}'"
    """);

var kernelArguments = new KernelArguments()
{
    ["input"] = "There is a tree directly in front of the car. Avoid it and then come back to the original path.",
    ["basic_moves"] = "forward, backward, turn left, turn right, and stop"
};

// Querying the prompt function
var response = await promptFunctionFromYaml.InvokeAsync(kernel, kernelArguments);

Console.WriteLine($"RENDERED PROMPT: {response.RenderedPrompt}"); // shows the rendered prompt of the prompt function
Console.WriteLine($"PROMPT RESPONSE: {response}");
