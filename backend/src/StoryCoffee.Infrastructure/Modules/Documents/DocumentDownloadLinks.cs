using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class DocumentDownloadLinks(IOptions<DocumentStorageOptions> options)
{
    private readonly DocumentStorageOptions options = options.Value;

    public PdfDownloadDto CreateDownloadDto(PdfDocumentResult pdf)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(options.PresignedUrlMinutes).ToUnixTimeSeconds();
        var signature = Sign(pdf.FileKey, expires);
        var downloadUrl = $"/api/files/download?fileKey={Uri.EscapeDataString(pdf.FileKey)}&fileName={Uri.EscapeDataString(pdf.FileName)}&expires={expires}&signature={Uri.EscapeDataString(signature)}";
        return new PdfDownloadDto(downloadUrl, pdf.FileName, pdf.FileKey, pdf.GeneratedAt);
    }

    public bool TryValidate(string fileKey, string signature, long expires, out string normalizedKey)
    {
        normalizedKey = string.Empty;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
        {
            return false;
        }

        try
        {
            normalizedKey = NormalizeKey(fileKey);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return FixedTimeEquals(signature, Sign(fileKey, expires));
    }

    public static string NormalizeKey(string fileKey)
    {
        var normalized = fileKey.Replace('\\', '/').TrimStart('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException("Invalid document file key.");
        }

        return string.Join('/', segments);
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
