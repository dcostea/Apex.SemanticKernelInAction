namespace RAGWithInMemoryAndPdf.Models;

internal sealed class RawContent
{
    public string? Text { get; init; }

    public ReadOnlyMemory<byte>? Image { get; init; }

    public int PageNumber { get; init; }
}
