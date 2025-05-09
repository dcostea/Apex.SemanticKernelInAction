namespace RAGWithInMemoryAndPdf.Services;

internal interface IDataLoader
{
    Task LoadPdfsAsync(string pdfDirectory);
}
