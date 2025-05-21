namespace Services;

internal interface IDataLoader
{
    Task LoadTextAsync(string txtDirectory);
}
