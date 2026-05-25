namespace StoryCoffee.Tests;

public sealed class TestDocumentStorageService : IDocumentStorageService
{
    private readonly Dictionary<string, StoredDocument> documents = new();

    public Task Save(string fileKey, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        documents[fileKey] = new StoredDocument(content, contentType, Path.GetFileName(fileKey));
        return Task.CompletedTask;
    }

    public PdfDownloadDto CreateDownloadDto(PdfDocumentResult pdf)
    {
        return new PdfDownloadDto($"/api/files/download?fileKey={Uri.EscapeDataString(pdf.FileKey)}", pdf.FileName, pdf.FileKey, pdf.GeneratedAt);
    }

    public Task<StoredDocument?> Get(string fileKey, string signature, long expires, CancellationToken cancellationToken)
    {
        documents.TryGetValue(fileKey, out var document);
        return Task.FromResult(document);
    }
}
