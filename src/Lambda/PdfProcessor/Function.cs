using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
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

    private readonly IObjectStore _store;

    public Function() : this(new S3ObjectStore(new AmazonS3Client()))
    {
    }

    public Function(IObjectStore store) => _store = store;

    public async Task HandleAsync(S3Event input, ILambdaContext context)
    {
        foreach (var (key, result) in await ProcessAsync(input))
        {
            context.Logger.LogInformation($"Processed {key}: {result}");
        }
    }

    public async Task<IReadOnlyList<(string Key, PdfValidationResult Result)>> ProcessAsync(S3Event input)
    {
        var results = new List<(string Key, PdfValidationResult Result)>();
        foreach (var record in input.Records)
        {
            var bucket = record.S3.Bucket.Name;
            // S3 event keys are URL-encoded, with spaces sent as '+'.
            var key = Uri.UnescapeDataString(record.S3.Object.Key.Replace('+', ' '));

            var result = await ValidateAsync(bucket, key, record.S3.Object.Size);
            await _store.TagAsync(bucket, key, new Dictionary<string, string>
            {
                [ProcessingTag] = "completed",
                [ValidationTag] = result == PdfValidationResult.Valid ? "passed" : "failed"
            });

            results.Add((key, result));
        }

        return results;
    }

    private async Task<PdfValidationResult> ValidateAsync(string bucket, string key, long sizeFromEvent)
    {
        // Reject oversized objects from the event metadata, before downloading them.
        if (sizeFromEvent > PdfRules.MaxBytes)
        {
            return PdfValidationResult.TooLarge;
        }

        var content = await _store.ReadAsync(bucket, key);
        return PdfRules.Validate(content);
    }
}
