namespace StoryCoffee.Infrastructure.Options;

public sealed class DocumentStorageOptions
{
    public string Provider { get; init; } = "Local";
    public string LocalRoot { get; init; } = ".storycoffee-files";
    public string SigningSecret { get; init; } = "dev-only-document-storage-signing-secret";
    public int PresignedUrlMinutes { get; init; } = 15;
    public string BucketName { get; init; } = "storycoffee-documents";
    public string S3Region { get; init; } = "ap-southeast-2";
    public string? ServiceUrl { get; init; }
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public bool ForcePathStyle { get; init; } = true;
}
