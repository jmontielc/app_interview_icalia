using System.Text.Json;
using Api.Integrations.Secrets;
using Api.Security;

namespace Api.Integrations.Bureaus;

/// <summary>Equifax wire format: flat JSON objects.</summary>
public sealed class EquifaxClient(HttpClient http, ISecretProvider secrets)
    : CreditBureauClientBase(http, secrets, "bureau/equifax/api-token")
{
    public override Bureau Bureau => Bureau.Equifax;

    protected override HttpRequestMessage BuildCreditReportRequest(Ssn ssn) =>
        new(HttpMethod.Post, "v1/credit-reports") { Content = JsonBody(new { ssn = ssn.Digits }) };

    protected override CreditReport MapCreditReport(JsonElement root) =>
        new(
            Bureau.Equifax,
            root.GetProperty("creditScore").GetInt32(),
            root.GetProperty("openAccounts").GetInt32(),
            root.GetProperty("totalBalance").GetDecimal(),
            DateTimeOffset.UtcNow);

    protected override HttpRequestMessage BuildEmploymentRequest(Ssn ssn) =>
        new(HttpMethod.Post, "v1/employment-verifications") { Content = JsonBody(new { ssn = ssn.Digits }) };

    protected override EmploymentStatus MapEmploymentStatus(JsonElement root) =>
        new(
            Bureau.Equifax,
            root.GetProperty("employmentStatus").GetString() == "ACTIVE",
            root.TryGetProperty("employer", out var employer) ? employer.GetString() : null,
            DateTimeOffset.UtcNow);
}
