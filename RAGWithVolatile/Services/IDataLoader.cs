// Copyright (c) Microsoft. All rights reserved.

namespace RAGWithInMemory.Services;

/// <summary>
/// Interface for loading data into a data store.
/// </summary>
internal interface IDataLoader
{
    Task LoadPdfsAsync(string pdfDirectory, int batchSize, int batchDelayInMs, CancellationToken cancellationToken);
}
