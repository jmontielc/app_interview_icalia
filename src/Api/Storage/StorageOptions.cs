namespace Api.Storage;

/// <summary>Bound from "Storage". KmsKeyId is optional. Without it, S3 uses the AWS-managed key.</summary>
public sealed class StorageOptions
{
    public string BucketName { get; set; } = string.Empty;

    public string? KmsKeyId { get; set; }

    /// <summary>
    /// Custom S3 endpoint (LocalStack in development). Empty in production. Set from "Aws:S3ServiceUrl" at startup.
    /// Presigned links use the same scheme as this endpoint.
    /// </summary>
    public string? S3ServiceUrl { get; set; }
}
