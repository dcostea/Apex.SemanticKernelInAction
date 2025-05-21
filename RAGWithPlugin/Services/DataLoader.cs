using Microsoft.Extensions.VectorData;
using Models;

namespace Services;

internal sealed class DataLoader(VectorStoreCollection<string, TextBlock> vectorStoreRecordCollection)
    : IDataLoader
{
    public async Task LoadTextAsync(string txtDirectory)
    {
        string[] txtFiles = Directory.GetFiles(txtDirectory, "*.txt");

        foreach (var txtFile in txtFiles)
        {
            string fileName = Path.GetFileName(txtFile);
            Console.WriteLine($"Loading {fileName}...");
            await IndexTextAsync(txtFile);
            Console.WriteLine($"Text {fileName} loading complete");
        }
    }

    private async Task IndexTextAsync(string txtFile)
    {
        var fileName = Path.GetFileName(txtFile);
        var absolutePath = new Uri(txtFile).AbsoluteUri;

        await vectorStoreRecordCollection.EnsureCollectionExistsAsync();

        var lines = File.ReadAllLines(txtFile);
        int totalLines = lines.Length;
        int lineNumber = 1;

        foreach (var line in lines)
        {
            Console.WriteLine($"  Processing line {lineNumber} of {totalLines}");

            // Map each paragraph to a TextBlock
            var textBlock = new TextBlock
            {
                Key = Guid.NewGuid().ToString(),
                Text = line,
                ReferenceDescription = $"{fileName}#page={lineNumber}",
                ReferenceLink = $"{absolutePath}#page={lineNumber}",
            };

            await vectorStoreRecordCollection.UpsertAsync(textBlock);
            Console.WriteLine($"  Upserted text block with key '{textBlock.Key}' into VectorDB");

            lineNumber++;
        }
    }
}
