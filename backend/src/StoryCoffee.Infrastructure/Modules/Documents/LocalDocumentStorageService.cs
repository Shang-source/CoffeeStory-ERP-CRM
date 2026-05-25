using Microsoft.Extensions.Options;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class LocalDocumentStorageService(IOptions<DocumentStorageOptions> options, DocumentDownloadLinks downloadLinks) : IDocumentStorageService
{
    private readonly DocumentStorageOptions options = options.Value;

    public async Task Save(string fileKey, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        var path = PathFor(fileKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
        await File.WriteAllTextAsync($"{path}.content-type", contentType, cancellationToken);
    }

    public PdfDownloadDto CreateDownloadDto(PdfDocumentResult pdf)
    {
        return downloadLinks.CreateDownloadDto(pdf);
    }

    public async Task<StoredDocument?> Get(string fileKey, string signature, long expires, CancellationToken cancellationToken)
    {
        if (!downloadLinks.TryValidate(fileKey, signature, expires, out var normalizedKey))
        {
            return null;
        }

        var path = PathFor(normalizedKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var content = await File.ReadAllBytesAsync(path, cancellationToken);
        var contentType = File.Exists($"{path}.content-type")
            ? await File.ReadAllTextAsync($"{path}.content-type", cancellationToken)
            : "application/octet-stream";
        return new StoredDocument(content, contentType, Path.GetFileName(fileKey));
    }

    private string PathFor(string fileKey)
    {
        var safeKey = DocumentDownloadLinks.NormalizeKey(fileKey);
        return Path.Combine(options.LocalRoot, safeKey);
    }
}
