namespace StoryCoffee.Infrastructure.Documents;

public sealed class DocumentStorageHealthCheck(IDocumentStorageService storage, ILogger<DocumentStorageHealthCheck> logger) : IDocumentStorageHealthCheck
{
    public async Task<bool> Check(CancellationToken cancellationToken)
    {
        try
        {
            var fileKey = $"health/{Guid.NewGuid():N}.txt";
            await storage.Save(fileKey, "ok"u8.ToArray(), "text/plain", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Document storage health check failed.");
            return false;
        }
    }
}
