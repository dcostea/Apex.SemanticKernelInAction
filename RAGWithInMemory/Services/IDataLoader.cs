namespace RAGWithInMemory.Services;

internal interface IDataLoader
{
    Task LoadTextAsync(string txtDirectory);
}
