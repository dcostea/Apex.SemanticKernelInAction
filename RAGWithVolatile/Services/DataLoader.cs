using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RAGWithInMemory.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace RAGWithInMemory.Services;

/// <summary>
/// Class that loads text from a PDF file into a vector store.
/// </summary>
/// <param name="vectorStoreRecordCollection">The collection to load the data into.</param>
/// <param name="chatCompletionService">The chat completion service to use for generating text from images.</param>
internal sealed class DataLoader(IVectorStoreRecordCollection<string, TextSnippet> vectorStoreRecordCollection,
    IChatCompletionService chatCompletionService) : IDataLoader
{
    /// <summary>
    /// Load the text from a PDF file into the data store.
    /// </summary>
    /// <param name="dataLoader"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task LoadPdfsAsync(string pdfDirectory, int batchSize, int batchDelayInMs, CancellationToken cancellationToken)
    {
        string[] pdfFiles = Directory.GetFiles(pdfDirectory, "*.pdf");

        foreach (var pdfFile in pdfFiles)
        {
            if (cancellationToken.IsCancellationRequested) break;
            string fileName = Path.GetFileName(pdfFile);
            Console.WriteLine($"Loading {fileName}...");
            await IndexPdfAsync(pdfFile, batchSize, batchDelayInMs, cancellationToken);
            Console.WriteLine($"PDF {fileName} loading complete\n");
        }
    }

    /// <summary>
    /// Index the text from a PDF file into the data store.
    /// </summary>
    /// <param name="pdfPath">The pdf file to index.</param>
    /// <param name="batchSize">Maximum number of parallel threads to generate embeddings and upload records.</param>
    /// <param name="batchDelayInMs">The number of milliseconds to delay between batches to avoid throttling.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>An async task that completes when the indexing is complete.</returns>
    private async Task IndexPdfAsync(string pdfPath, int batchSize, int batchDelayInMs, CancellationToken cancellationToken)
    {
        await vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync(cancellationToken);

        var sections = LoadTextAndImages(pdfPath, cancellationToken);

        // Process each batch of content items.
        foreach (var batch in sections.Chunk(batchSize))
        {
            if (cancellationToken.IsCancellationRequested) break;

            var textContentTasks = batch.Select(async content =>
            {
                if (content.Text != null)
                {
                    return content;
                }

                if (content.Image != null)
                {
                    var textFromImage = await ConvertImageToTextAsync(
                        chatCompletionService,
                        content.Image.Value,
                        cancellationToken);

                    return new RawContent
                    {
                        Text = textFromImage,
                        PageNumber = content.PageNumber
                    };
                }

                // Return a default value if neither Text nor Image is present.
                return new RawContent
                {
                    Text = string.Empty,
                    PageNumber = content.PageNumber
                };
            });
            var textContent = await Task.WhenAll(textContentTasks);

            var fileName = Path.GetFileName(pdfPath);
            var absolutePath = new Uri(pdfPath).AbsoluteUri;

            // Map each paragraph to a TextSnippet.
            var records = textContent.Select(content => new TextSnippet
            {
                Key = Guid.NewGuid().ToString(),
                Text = content.Text,
                ReferenceDescription = $"{fileName}#page={content.PageNumber}",
                ReferenceLink = $"{absolutePath}#page={content.PageNumber}",
            });

            // Upsert the records into the vector store.
            var upsertedKeys = await vectorStoreRecordCollection.UpsertAsync(records, cancellationToken: cancellationToken);
            foreach (var key in upsertedKeys)
            {
                Console.WriteLine($"Upserted record '{key}' into VectorDB");
            }

            await Task.Delay(batchDelayInMs, cancellationToken);
        }
    }

    /// <summary>
    /// Read the text and images from each page in the provided PDF file.
    /// </summary>
    /// <param name="pdfPath">The pdf file to read the text and images from.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The text and images from the pdf file, plus the page number that each is on.</returns>
    private static IEnumerable<RawContent> LoadTextAndImages(string pdfPath, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(pdfPath);
        int totalPages = document.NumberOfPages;

        foreach (var page in document.GetPages())
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            Console.WriteLine($"Processing page {page.Number} of {totalPages}");

            foreach (var image in page.GetImages())
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                if (image.TryGetPng(out var png))
                {
                    yield return new RawContent { Image = png, PageNumber = page.Number };
                }
                else
                {
                    Console.WriteLine($"Skipping unsupported image format on page {page.Number}");
                }
            }
            var blocks = DefaultPageSegmenter.Instance.GetBlocks(page.GetWords());
            foreach (var block in blocks)
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                yield return new RawContent { Text = block.Text, PageNumber = page.Number };
            }
        }
    }

    /// <summary>
    /// Convert image to text using chat completion service.
    /// </summary>
    /// <param name="chatCompletionService">The chat completion service to use for generating text from images.</param>
    /// <param name="imageBytes">The image to generate the text for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated text.</returns>
    private static async Task<string> ConvertImageToTextAsync(
        IChatCompletionService chatCompletionService,
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage([
            new TextContent("Describe the content of this image in detail:"),
            new ImageContent(imageBytes, "image/png"),
        ]);
        var result = await chatCompletionService.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken);
        return string.Join("\n", result.Select(r => r.Content));
    }
}
