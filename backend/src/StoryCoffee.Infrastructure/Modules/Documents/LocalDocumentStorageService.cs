using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class LocalDocumentStorageService(IOptions<DocumentStorageOptions> options) : IDocumentStorageService
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
        var expires = DateTimeOffset.UtcNow.AddMinutes(options.PresignedUrlMinutes).ToUnixTimeSeconds();
        var signature = Sign(pdf.FileKey, expires);
        var downloadUrl = $"/api/files/download?fileKey={Uri.EscapeDataString(pdf.FileKey)}&fileName={Uri.EscapeDataString(pdf.FileName)}&expires={expires}&signature={Uri.EscapeDataString(signature)}";
        return new PdfDownloadDto(downloadUrl, pdf.FileName, pdf.FileKey, pdf.GeneratedAt);
    }

    public async Task<StoredDocument?> Get(string fileKey, string signature, long expires, CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires || !FixedTimeEquals(signature, Sign(fileKey, expires)))
        {
            return null;
        }

        var path = PathFor(fileKey);
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
        var safeKey = fileKey.Replace('\\', '/').TrimStart('/');
        return Path.Combine(options.LocalRoot, safeKey);
    }

    private string Sign(string fileKey, long expires)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.SigningSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{fileKey}|{expires}"))).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
