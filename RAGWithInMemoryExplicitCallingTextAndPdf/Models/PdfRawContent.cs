namespace Models;

internal sealed class PdfRawContent
{
    public string? Text { get; init; }

    public ReadOnlyMemory<byte>? Image { get; init; }

    public int PageNumber { get; init; }
}
