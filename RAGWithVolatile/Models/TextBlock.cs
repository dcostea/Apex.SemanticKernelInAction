using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace RAGWithInMemoryAndPdf.Models;

internal sealed class TextBlock
{
    [VectorStoreRecordKey]
    public required string Key { get; set; }

    [TextSearchResultValue]
    [VectorStoreRecordData]
    public string? Text { get; set; }

    [TextSearchResultName]
    [VectorStoreRecordData]
    public string? ReferenceDescription { get; set; }

    [TextSearchResultLink]
    [VectorStoreRecordData]
    public string? ReferenceLink { get; set; }

    [VectorStoreRecordVector(1536)]
    public string? TextEmbedding => Text;
}
