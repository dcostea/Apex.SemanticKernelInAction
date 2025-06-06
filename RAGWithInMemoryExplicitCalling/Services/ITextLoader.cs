namespace Services;

internal interface ITextLoader
{
    Task LoadAsync(string txtDirectory);
}
