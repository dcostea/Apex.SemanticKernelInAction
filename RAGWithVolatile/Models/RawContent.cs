namespace RAGWithInMemory.Models;

/// <summary>
/// Private model for returning the content items from a PDF file.
/// </summary>
internal sealed class RawContent
{
    public string? Text { get; init; }

    public ReadOnlyMemory<byte>? Image { get; init; }

    public int PageNumber { get; init; }
}
