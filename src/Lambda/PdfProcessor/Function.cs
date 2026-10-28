using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.S3.Model;
using AppInterview.Pdf;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace PdfProcessor;

/// <summary>
/// Triggered by S3 ObjectCreated events on the documents bucket. Re-checks each uploaded PDF
/// and records the outcome as object tags. The API reads those tags to report status.
/// The object is not moved or deleted here.
/// </summary>
public sealed class Function
{
    public const string ProcessingTag = "processing";
    public const string ValidationTag = "validation";

    private readonly IAmazonS3 _s3;

    public Function() : this(new AmazonS3Client())
    {
    }

    public Function(IAmazonS3 s3) => _s3 = s3;

    public async Task HandleAsync(S3Event input, ILambdaContext context)
    {
        foreach (var record in input.Records)
        {
            var bucket = record.S3.Bucket.Name;
            // S3 event keys are URL-encoded, with spaces sent as '+'.
            var key = Uri.UnescapeDataString(record.S3.Object.Key.Replace('+', ' '));

            var result = await ValidateObjectAsync(bucket, key);
            context.Logger.LogInformation($"Processed {key}: {result}");

            await _s3.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = bucket,
                Key = key,
                Tagging = new Tagging
                {
                    TagSet =
                    [
                        new Tag { Key = ProcessingTag, Value = "completed" },
                        new Tag { Key = ValidationTag, Value = result == PdfValidationResult.Valid ? "passed" : "failed" }
                    ]
                }
            });
        }
    }

    private async Task<PdfValidationResult> ValidateObjectAsync(string bucket, string key)
    {
        using var response = await _s3.GetObjectAsync(bucket, key);
        if (response.Headers.ContentLength > PdfRules.MaxBytes)
        {
            return PdfValidationResult.TooLarge;
        }

        using var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory);
        return PdfRules.Validate(memory.GetBuffer().AsSpan(0, (int)memory.Length));
    }
}
