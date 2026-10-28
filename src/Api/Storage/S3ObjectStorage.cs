using Amazon.S3;
using Amazon.S3.Model;

namespace Api.Storage;

/// <summary>
/// S3 storage. Objects are written with SSE-KMS. Reads happen only through presigned links
/// that expire after at most five minutes. The bucket is private, with public access blocked at the infrastructure level.
/// </summary>
public sealed class S3ObjectStorage(IAmazonS3 s3, StorageOptions options, TimeProvider clock) : IObjectStorage
{
    public static readonly TimeSpan MaxPresignLifetime = TimeSpan.FromMinutes(5);

    public async Task PutAsync(string key, byte[] content, string contentType, IReadOnlyDictionary<string, string> tags, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = key,
            InputStream = new MemoryStream(content),
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
            TagSet = tags.Select(t => new Tag { Key = t.Key, Value = t.Value }).ToList()
        };

        if (!string.IsNullOrEmpty(options.KmsKeyId))
        {
            request.ServerSideEncryptionKeyManagementServiceKeyId = options.KmsKeyId;
        }

        await s3.PutObjectAsync(request, ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetTagsAsync(string key, CancellationToken ct = default)
    {
        var response = await s3.GetObjectTaggingAsync(new GetObjectTaggingRequest
        {
            BucketName = options.BucketName,
            Key = key
        }, ct);

        return response.Tagging.ToDictionary(t => t.Key, t => t.Value);
    }

    public string CreatePresignedGetUrl(string key, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero || lifetime > MaxPresignLifetime)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Presigned links last at most five minutes.");
        }

        return s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = clock.GetUtcNow().UtcDateTime.Add(lifetime),
            // The SDK defaults to HTTPS. A plain-HTTP emulator would then hand out links that fail.
            Protocol = IsPlainHttpEndpoint ? Protocol.HTTP : Protocol.HTTPS
        });
    }

    private bool IsPlainHttpEndpoint =>
        options.S3ServiceUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true;
}
