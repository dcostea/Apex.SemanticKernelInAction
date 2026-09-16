using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServerWithHttp.Resources;

[McpServerResourceType]
public class MotorsResources
{
    [McpServerResource(UriTemplate = "resource://mcp/bio", Name = "bio", MimeType = "text/plain")]
    [Description("A static resource containing the robot's bio.")]
    public static string Bio() => "My name is Robby, the robot.";

    [McpServerResource(UriTemplate = "resource://mcp/greet/{name}", Name = "greet", MimeType = "text/plain")]
    [Description("A resource template returning a personalised greeting.")]
    public static TextResourceContents Greet(
        RequestContext<ReadResourceRequestParams> context,
        [Description("The name to greet.")] string name) => new()
        {
            Uri = context.Params!.Uri,
            MimeType = "text/plain",
            Text = $"Hello, {name}! I am Robby, the robot.",
        };
}
