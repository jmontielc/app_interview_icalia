using Api.Storage;

namespace Api.Tests.Endpoints;

/// <summary>Stands in for S3. Stores objects and tags in memory and records presign requests.</summary>
public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly Dictionary<string, (byte[] Content, string ContentType, Dictionary<string, string> Tags)> _objects = new();

    public List<TimeSpan> PresignLifetimes { get; } = [];

    public Task PutAsync(string key, byte[] content, string contentType, IReadOnlyDictionary<string, string> tags, CancellationToken ct = default)
    {
        _objects[key] = (content, contentType, new Dictionary<string, string>(tags));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, string>> GetTagsAsync(string key, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(_objects[key].Tags));

    public string CreatePresignedGetUrl(string key, TimeSpan lifetime)
    {
        if (lifetime > S3ObjectStorage.MaxPresignLifetime)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }

        PresignLifetimes.Add(lifetime);
        return $"https://storage.test/{key}?X-Amz-Expires={(int)lifetime.TotalSeconds}";
    }

    /// <summary>Simulates the PdfProcessor Lambda writing a result tag.</summary>
    public void SetTag(string key, string name, string value) => _objects[key].Tags[name] = value;

    public IEnumerable<string> Keys => _objects.Keys;
}
