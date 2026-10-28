using System.Collections.Concurrent;

namespace Api.Integrations.Secrets;

/// <summary>
/// Decorator that caches secrets for a short TTL, so each request does not call Secrets Manager.
/// Cached values are never logged.
/// </summary>
public sealed class CachedSecretProvider(ISecretProvider inner, TimeProvider clock, TimeSpan ttl) : ISecretProvider
{
    private readonly ConcurrentDictionary<string, (string Value, DateTimeOffset Expiry)> _cache = new();

    public async Task<string> GetSecretAsync(string name, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        if (_cache.TryGetValue(name, out var entry) && entry.Expiry > now)
        {
            return entry.Value;
        }

        var value = await inner.GetSecretAsync(name, ct);
        _cache[name] = (value, now + ttl);
        return value;
    }
}
