using Microsoft.Extensions.Configuration;

namespace Api.Integrations.Secrets;

/// <summary>
/// Local development provider. Reads "Secrets:{name}" from configuration,
/// typically populated with `dotnet user-secrets`. Not for production.
/// </summary>
public sealed class ConfigSecretProvider(IConfiguration configuration) : ISecretProvider
{
    public Task<string> GetSecretAsync(string name, CancellationToken ct = default)
    {
        var value = configuration[$"Secrets:{name}"];
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Secret '{name}' is not configured.");
        }

        return Task.FromResult(value);
    }
}
