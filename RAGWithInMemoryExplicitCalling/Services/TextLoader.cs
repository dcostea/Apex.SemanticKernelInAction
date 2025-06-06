using Microsoft.Extensions.VectorData;
using Models;

namespace Services;

internal sealed class TextLoader(VectorStoreCollection<string, TextBlock> vectorStoreCollection)
    : ITextLoader
{
    public async Task LoadAsync(string txtDirectory)
    {
        string[] txtFiles = Directory.GetFiles(txtDirectory, "*.txt");

        foreach (var txtFile in txtFiles)
        {
            string fileName = Path.GetFileName(txtFile);
            Console.WriteLine($"Loading {fileName}...");
            await IndexAsync(txtFile);
            Console.WriteLine($"Text {fileName} loading complete");
        }
    }

    private async Task IndexAsync(string txtFile)
    {
        var fileName = Path.GetFileName(txtFile);
        var absolutePath = txtFile;

        await vectorStoreCollection.EnsureCollectionExistsAsync();

        var text = await File.ReadAllTextAsync(txtFile);
        var paragraphs = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        int totalParagraphs = paragraphs.Length;
        int paragraphNumber = 1;

        foreach (var paragraph in paragraphs)
        {
            Console.WriteLine($"  Processing paragraph {paragraphNumber} of {totalParagraphs}");

            var textBlock = new TextBlock
            {
                Key = Guid.NewGuid().ToString(),
                Text = paragraph.Trim(),
                ReferenceDescription = $"{fileName}#paragraph={paragraphNumber}",
                ReferenceLink = $"{absolutePath}#paragraph={paragraphNumber}",
            };

            await vectorStoreCollection.UpsertAsync(textBlock);
            Console.WriteLine($"  Upserted text block with key '{textBlock.Key}'");

            paragraphNumber++;
        }
    }
}
