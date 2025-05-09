using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace RAGWithInMemory.Models;

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

    /// <summary>
    /// The text embedding for this snippet. This is used to search the vector store.
    /// While this is a string property it has the vector attribute, which means whatever
    /// text it contains will be converted to a vector and stored as a vector in the vector store.
    /// </summary>
    [VectorStoreRecordVector(1536)]
    public string? TextEmbedding => Text;
}
