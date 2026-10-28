namespace Api.Storage;

/// <summary>Bound from "Storage". KmsKeyId is optional. Without it, S3 uses the AWS-managed key.</summary>
public sealed class StorageOptions
{
    public string BucketName { get; set; } = string.Empty;

    public string? KmsKeyId { get; set; }
}
