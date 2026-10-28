using System.Net.Http.Headers;
using System.Text.Json;
using Api.Integrations.Secrets;
using Api.Security;

namespace Api.Integrations.Bureaus;

/// <summary>
/// Template for bureau calls. Owns the shared work: fetching the bearer token from
/// <see cref="ISecretProvider"/>, sending the request, mapping HTTP errors, and parsing JSON.
/// Subclasses supply only the bureau-specific request shape and response mapping.
/// </summary>
public abstract class CreditBureauClientBase(HttpClient http, ISecretProvider secrets, string tokenSecretName)
    : ICreditBureauClient
{
    public abstract Bureau Bureau { get; }

    public Task<CreditReport> GetCreditReportAsync(Ssn ssn, CancellationToken ct = default) =>
        ExecuteAsync(BuildCreditReportRequest(ssn), MapCreditReport, ct);

    public Task<EmploymentStatus> GetEmploymentStatusAsync(Ssn ssn, CancellationToken ct = default) =>
        ExecuteAsync(BuildEmploymentRequest(ssn), MapEmploymentStatus, ct);

    protected abstract HttpRequestMessage BuildCreditReportRequest(Ssn ssn);

    protected abstract CreditReport MapCreditReport(JsonElement root);

    protected abstract HttpRequestMessage BuildEmploymentRequest(Ssn ssn);

    protected abstract EmploymentStatus MapEmploymentStatus(JsonElement root);

    private async Task<T> ExecuteAsync<T>(HttpRequestMessage request, Func<JsonElement, T> map, CancellationToken ct)
    {
        var token = await secrets.GetSecretAsync(tokenSecretName, ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new BureauException(Bureau, (int)response.StatusCode, "unexpected HTTP status");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        try
        {
            return map(doc.RootElement);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or ArgumentException or FormatException)
        {
            throw new BureauException(Bureau, null, "response did not match the expected shape", ex);
        }
    }

    /// <summary>JSON body that carries the SSN. Sent only over TLS in the request body, never in the URL.</summary>
    protected static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
}
