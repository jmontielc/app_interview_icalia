using Amazon.S3;
using Amazon.S3.Model;

namespace PdfProcessor;

/// <summary>Production adapter. Uses the Lambda's execution role for S3 access.</summary>
public sealed class S3ObjectStore(IAmazonS3 s3) : IObjectStore
{
    public async Task<byte[]> ReadAsync(string bucket, string key)
    {
        using var response = await s3.GetObjectAsync(bucket, key);
        using var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory);
        return memory.ToArray();
    }

    public Task TagAsync(string bucket, string key, IReadOnlyDictionary<string, string> tags) =>
        s3.PutObjectTaggingAsync(new PutObjectTaggingRequest
        {
            BucketName = bucket,
            Key = key,
            Tagging = new Tagging
            {
                TagSet = tags.Select(t => new Tag { Key = t.Key, Value = t.Value }).ToList()
            }
        });
}
