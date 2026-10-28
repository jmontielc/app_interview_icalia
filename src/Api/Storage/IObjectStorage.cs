namespace Api.Storage;

/// <summary>
/// Object storage for documents. The S3 implementation enforces private access, SSE-KMS and short-lived links.
/// </summary>
public interface IObjectStorage
{
    Task PutAsync(string key, byte[] content, string contentType, IReadOnlyDictionary<string, string> tags, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, string>> GetTagsAsync(string key, CancellationToken ct = default);

    /// <summary>GET link valid for at most <see cref="S3ObjectStorage.MaxPresignLifetime"/>.</summary>
    string CreatePresignedGetUrl(string key, TimeSpan lifetime);
}
