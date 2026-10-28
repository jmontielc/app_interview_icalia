namespace Api.Integrations.Bureaus;

/// <summary>
/// Raised when a bureau call fails. Carries the status code only. The response body
/// is never copied in, since it may echo personal data.
/// </summary>
public sealed class BureauException(Bureau bureau, int? statusCode, string reason, Exception? inner = null)
    : Exception($"{bureau} request failed: {reason}.", inner)
{
    public Bureau Bureau { get; } = bureau;

    public int? StatusCode { get; } = statusCode;
}
