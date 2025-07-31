using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

namespace Plugins;

[Description("Text search plugin.")]
public class SearchPlugin
{
    const string SourceFilePath = @"Data";

    [KernelFunction, Description("Search for more text context. Returns found context.")]
    public static async Task<string> GetTextSearchResults()
    {
        string[] txtFiles = Directory.GetFiles(SourceFilePath, "*.txt");
        var context = new StringBuilder();

        foreach (var txtFile in txtFiles)
        {
            var textFileName = Path.GetFileName(txtFile);

            try
            {
                Console.WriteLine($"Loading {textFileName}...");
                var text = await File.ReadAllTextAsync(txtFile);
                context.Append(text);
                Console.WriteLine($"Text {textFileName} loading complete");
            }
            catch (Exception)
            {
                Console.WriteLine($"Text {textFileName} reading error!");
            }
        }

        return await Task.FromResult(context.ToString());
    }
}
