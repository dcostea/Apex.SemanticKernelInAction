namespace Services;

internal interface IPdfLoader
{
    Task LoadAsync(string pdfDirectory);
}
