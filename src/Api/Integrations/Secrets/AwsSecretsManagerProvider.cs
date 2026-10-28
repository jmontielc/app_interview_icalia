using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Api.Integrations.Secrets;

/// <summary>
/// Reads secrets from AWS Secrets Manager. The API's IAM role supplies credentials,
/// so no keys are stored in the app. Locally it points at LocalStack.
/// </summary>
public sealed class AwsSecretsManagerProvider(IAmazonSecretsManager client) : ISecretProvider
{
    public async Task<string> GetSecretAsync(string name, CancellationToken ct = default)
    {
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = name }, ct);

        // The error names the secret, never its value.
        return response.SecretString
            ?? throw new InvalidOperationException($"Secret '{name}' has no string value.");
    }
}
