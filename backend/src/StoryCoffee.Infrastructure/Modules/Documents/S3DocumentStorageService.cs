using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using System.Net;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class S3DocumentStorageService : IDocumentStorageService, IDisposable
{
    private readonly DocumentStorageOptions options;
    private readonly DocumentDownloadLinks downloadLinks;
    private readonly IAmazonS3 s3;

    public S3DocumentStorageService(IOptions<DocumentStorageOptions> options, DocumentDownloadLinks downloadLinks)
    {
        this.options = options.Value;
        this.downloadLinks = downloadLinks;
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
            Key = DocumentDownloadLinks.NormalizeKey(fileKey),
            InputStream = stream,
            ContentType = contentType
        }, cancellationToken);
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

        try
        {
            using var response = await s3.GetObjectAsync(options.BucketName, normalizedKey, cancellationToken);
            await using var responseStream = response.ResponseStream;
            using var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream, cancellationToken);
            return new StoredDocument(
                memoryStream.ToArray(),
                response.Headers.ContentType ?? "application/octet-stream",
                Path.GetFileName(normalizedKey));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
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

}
