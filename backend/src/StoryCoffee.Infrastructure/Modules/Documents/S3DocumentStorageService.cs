using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class S3DocumentStorageService : IDocumentStorageService, IDisposable
{
    private readonly DocumentStorageOptions options;
    private readonly IAmazonS3 s3;

    public S3DocumentStorageService(IOptions<DocumentStorageOptions> options)
    {
        this.options = options.Value;
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(this.options.S3Region),
            ForcePathStyle = this.options.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(this.options.ServiceUrl))
        {
            config.ServiceURL = this.options.ServiceUrl;
            config.AuthenticationRegion = this.options.S3Region;
        }

        if (!string.IsNullOrWhiteSpace(this.options.AccessKey) && !string.IsNullOrWhiteSpace(this.options.SecretKey))
        {
            s3 = new AmazonS3Client(new BasicAWSCredentials(this.options.AccessKey, this.options.SecretKey), config);
        }
        else
        {
            s3 = new AmazonS3Client(config);
        }
    }

    public async Task Save(string fileKey, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucket(cancellationToken);
        await using var stream = new MemoryStream(content);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = NormalizeKey(fileKey),
            InputStream = stream,
            ContentType = contentType
        }, cancellationToken);
    }

    public PdfDownloadDto CreateDownloadDto(PdfDocumentResult pdf)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = NormalizeKey(pdf.FileKey),
            Expires = DateTime.UtcNow.AddMinutes(options.PresignedUrlMinutes),
            Verb = HttpVerb.GET
        };
        return new PdfDownloadDto(s3.GetPreSignedURL(request), pdf.FileName, pdf.FileKey, pdf.GeneratedAt);
    }

    public async Task<StoredDocument?> Get(string fileKey, string signature, long expires, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return null;
    }

    public void Dispose()
    {
        s3.Dispose();
    }

    private async Task EnsureBucket(CancellationToken cancellationToken)
    {
        try
        {
            await s3.GetBucketLocationAsync(options.BucketName, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await s3.PutBucketAsync(new PutBucketRequest
            {
                BucketName = options.BucketName,
                BucketRegionName = options.S3Region
            }, cancellationToken);
        }
    }

    private static string NormalizeKey(string fileKey)
    {
        return fileKey.Replace('\\', '/').TrimStart('/');
    }
}
