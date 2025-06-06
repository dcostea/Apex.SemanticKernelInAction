﻿using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace Services;

internal sealed class PdfLoader(VectorStoreCollection<string, TextBlock> vectorStoreCollection,
    IChatCompletionService chatCompletionService) : IPdfLoader
{
    public async Task LoadAsync(string ragFilesDirectory)
    {
        string[] pdfFiles = Directory.GetFiles(ragFilesDirectory, "*.pdf");

        foreach (var pdfFile in pdfFiles)
        {
            string fileName = Path.GetFileName(pdfFile);
            Console.WriteLine($"Loading {fileName}...");
            await IndexAsync(pdfFile);
            Console.WriteLine($"PDF {fileName} loading complete");
        }
    }

    private async Task IndexAsync(string pdfFile)
    {
        var fileName = Path.GetFileName(pdfFile);
        var absolutePath = pdfFile;

        await vectorStoreCollection.EnsureCollectionExistsAsync();

        foreach (var rawContent in ReadRawContentsFromPdf(pdfFile))
        {
            PdfRawContent? textContent = rawContent.Image != null
                ? await ExtractTextFromImageAsync(rawContent)
                : rawContent;

            if (textContent is null)
            {
                Console.WriteLine($"  Skipping unsupported content on page {rawContent.PageNumber}");
                continue;
            }

            // Map the processed content to a TextBlock
            var textBlock = new TextBlock
            {
                Key = Guid.NewGuid().ToString(),
                Text = textContent.Text,
                ReferenceDescription = $"{fileName}#page={textContent.PageNumber}",
                ReferenceLink = $"{absolutePath}#page={textContent.PageNumber}",
            };

            await vectorStoreCollection.UpsertAsync(textBlock);
            Console.WriteLine($"    Upserted text block with key '{textBlock.Key}'");
        }
    }

    private static IEnumerable<PdfRawContent> ReadRawContentsFromPdf(string pdfFile)
    {
        using var document = PdfDocument.Open(pdfFile);
        int totalPages = document.NumberOfPages;

        foreach (var page in document.GetPages())
        {
            Console.WriteLine($"  Read page {page.Number} of {totalPages}");

            foreach (var image in page.GetImages())
            {
                if (image.TryGetPng(out var pngImageBytes))
                {
                    Console.WriteLine($"    Read image of {pngImageBytes.Length} bytes in page {page.Number}");
                    yield return new PdfRawContent { Image = pngImageBytes, PageNumber = page.Number };
                }
                else
                {
                    Console.WriteLine($"    Skipping unsupported image format on page {page.Number}");
                }
            }

            foreach (var block in DefaultPageSegmenter.Instance.GetBlocks(page.GetWords()))
            {
                Console.WriteLine($"    Read text block of {block.Text.Length} chars in page {page.Number} of {totalPages}");
                yield return new PdfRawContent { Text = block.Text, PageNumber = page.Number };
            }
        }
    }

    private async Task<PdfRawContent?> ExtractTextFromImageAsync(PdfRawContent content)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage([
            new TextContent("Describe the content of this image in detail:"),
            new ImageContent(content.Image!.Value, "image/png"),
        ]);
        var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory);
        Console.WriteLine($"      Extracted text from image: {result.Content}");
        return new PdfRawContent
        {
            Text = result.Content,
            PageNumber = content.PageNumber
        };
    }
}
