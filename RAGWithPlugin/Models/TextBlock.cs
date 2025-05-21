using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Models;

internal sealed class TextBlock
{
    [VectorStoreKey]
    public required string Key { get; set; }

    [VectorStoreData]
    [TextSearchResultValue]
    public string? Text { get; set; }

    [VectorStoreData]
    [TextSearchResultName]
    public string? ReferenceDescription { get; set; }

    [VectorStoreData]
    [TextSearchResultLink]
    public string? ReferenceLink { get; set; }

    [VectorStoreVector(1536)]
    public string? TextEmbedding => Text;
}
