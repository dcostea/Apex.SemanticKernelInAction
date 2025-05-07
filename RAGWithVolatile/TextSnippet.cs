// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;

namespace RAGWithVolatile;

/// <summary>
/// Data model for storing a section of text with an embedding and an optional reference link.
/// </summary>
internal sealed class TextSnippet
{
    [VectorStoreRecordKey]
    public required string Key { get; set; }

    [VectorStoreRecordData]
    public string? Text { get; set; }

    [VectorStoreRecordData]
    public string? ReferenceDescription { get; set; }

    [VectorStoreRecordData]
    public string? ReferenceLink { get; set; }

    [VectorStoreRecordVector(1536)]
    public ReadOnlyMemory<float> TextEmbedding { get; set; }
}
