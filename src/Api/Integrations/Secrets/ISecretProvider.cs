namespace Api.Integrations.Secrets;

/// <summary>
/// Abstraction over where secrets live (AWS Secrets Manager in AWS, configuration locally).
/// Callers ask for a secret by name and never handle the storage details.
/// </summary>
public interface ISecretProvider
{
    Task<string> GetSecretAsync(string name, CancellationToken ct = default);
}
