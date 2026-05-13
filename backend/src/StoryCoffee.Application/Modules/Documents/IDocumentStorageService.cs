namespace StoryCoffee.Application.Documents;

public interface IPdfGenerator
{
    byte[] Generate(PdfDocumentResult document);
}

public interface IDocumentStorageService
{
    Task Save(string fileKey, byte[] content, string contentType, CancellationToken cancellationToken);
    PdfDownloadDto CreateDownloadDto(PdfDocumentResult pdf);
    Task<StoredDocument?> Get(string fileKey, string signature, long expires, CancellationToken cancellationToken);
}

public sealed record StoredDocument(byte[] Content, string ContentType, string FileName);
