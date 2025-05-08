using System.Net;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RAGWithVolatile.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace RAGWithVolatile.Services;

/// <summary>
/// Class that loads text from a PDF file into a vector store.
/// </summary>
/// <param name="vectorStoreRecordCollection">The collection to load the data into.</param>
/// <param name="chatCompletionService">The chat completion service to use for generating text from images.</param>
internal sealed class DataLoader(
    IVectorStoreRecordCollection<string, TextSnippet> vectorStoreRecordCollection,
    IChatCompletionService chatCompletionService) : IDataLoader
{
    public async Task LoadPdf(string pdfPath, int batchSize, int betweenBatchDelayInMs, CancellationToken cancellationToken)
    {
        // Create the collection if it doesn't exist.
        await vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync(cancellationToken);

        // Load the text and images from the PDF file and split them into batches.
        var sections = LoadTextAndImages(pdfPath, cancellationToken);
        var batches = sections.Chunk(batchSize);

        // Process each batch of content items.
        foreach (var batch in batches)
        {
            // Convert any images to text.
            var textContentTasks = batch.Select(async content =>
            {
                if (content.Text != null)
                {
                    return content;
                }

                var textFromImage = await ConvertImageToTextWithRetryAsync(
                    chatCompletionService,
                    content.Image!.Value,
                    cancellationToken).ConfigureAwait(false);
                return new RawContent { Text = textFromImage, PageNumber = content.PageNumber };
            });
            var textContent = await Task.WhenAll(textContentTasks).ConfigureAwait(false);

            // Map each paragraph to a TextSnippet and generate an embedding for it.
            var records = textContent.Select(content => new TextSnippet
            {
                Key = Guid.NewGuid().ToString(),
                Text = content.Text,
                ReferenceDescription = $"{new FileInfo(pdfPath).Name}#page={content.PageNumber}",
                ReferenceLink = $"{new Uri(new FileInfo(pdfPath).FullName).AbsoluteUri}#page={content.PageNumber}",
            });

            // Upsert the records into the vector store.
            var upsertedKeys = await vectorStoreRecordCollection.UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var key in upsertedKeys)
            {
                Console.WriteLine($"Upserted record '{key}' into VectorDB");
            }

            await Task.Delay(betweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task IndexPdfs(CancellationToken cancellationToken)
    {
        // Directory to scan for PDFs
        string pdfDirectory = @"C:\Temp\PDFs";

        // Find all PDF files
        string[] pdfFiles = Directory.GetFiles(pdfDirectory, "*.pdf");

        if (pdfFiles.Length == 0)
        {
            Console.WriteLine($"No PDF files found in {pdfDirectory}");
            return;
        }

        // Load each PDF file
        foreach (var pdfFile in pdfFiles)
        {
            string fileName = Path.GetFileName(pdfFile);
            Console.WriteLine($"Indexing {fileName}...");
            await LoadPdf(pdfFile, 2, 1000, cancellationToken);
            Console.WriteLine($"PDF {fileName} indexing complete\n");
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
        using PdfDocument document = PdfDocument.Open(pdfPath);
        foreach (Page page in document.GetPages())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            foreach (var image in page.GetImages())
            {
                if (image.TryGetPng(out var png))
                {
                    yield return new RawContent { Image = png, PageNumber = page.Number };
                }
                else
                {
                    Console.WriteLine($"Unsupported image format on page {page.Number}");
                }
            }

            var blocks = DefaultPageSegmenter.Instance.GetBlocks(page.GetWords());
            foreach (var block in blocks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                yield return new RawContent { Text = block.Text, PageNumber = page.Number };
            }
        }
    }

    /// <summary>
    /// Add a simple retry mechanism to image to text.
    /// </summary>
    /// <param name="chatCompletionService">The chat completion service to use for generating text from images.</param>
    /// <param name="imageBytes">The image to generate the text for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated text.</returns>
    private static async Task<string> ConvertImageToTextWithRetryAsync(
        IChatCompletionService chatCompletionService,
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage([
                    new TextContent("What’s in this image?"),
                    new ImageContent(imageBytes, "image/png"),
                ]);
                var result = await chatCompletionService.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken).ConfigureAwait(false);
                return string.Join("\n", result.Select(x => x.Content));
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    Console.WriteLine($"Failed to generate text from image. Error: {ex}");
                    Console.WriteLine("Retrying text to image conversion...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
