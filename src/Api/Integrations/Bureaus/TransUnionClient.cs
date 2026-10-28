using System.Text.Json;
using Api.Integrations.Secrets;
using Api.Security;

namespace Api.Integrations.Bureaus;

/// <summary>TransUnion wire format: nested "data" envelope with different field names.</summary>
public sealed class TransUnionClient(HttpClient http, ISecretProvider secrets)
    : CreditBureauClientBase(http, secrets, "bureau/transunion/api-token")
{
    public override Bureau Bureau => Bureau.TransUnion;

    protected override HttpRequestMessage BuildCreditReportRequest(Ssn ssn) =>
        new(HttpMethod.Post, "v2/reports/credit") { Content = JsonBody(new { subject = new { ssn = ssn.Digits } }) };

    protected override CreditReport MapCreditReport(JsonElement root)
    {
        var data = root.GetProperty("data");
        return new CreditReport(
            Bureau.TransUnion,
            data.GetProperty("score").GetProperty("value").GetInt32(),
            data.GetProperty("tradelines").GetProperty("openCount").GetInt32(),
            data.GetProperty("balances").GetProperty("total").GetDecimal(),
            DateTimeOffset.UtcNow);
    }

    protected override HttpRequestMessage BuildEmploymentRequest(Ssn ssn) =>
        new(HttpMethod.Post, "v2/employment/verify") { Content = JsonBody(new { subject = new { ssn = ssn.Digits } }) };

    protected override EmploymentStatus MapEmploymentStatus(JsonElement root)
    {
        var data = root.GetProperty("data");
        return new EmploymentStatus(
            Bureau.TransUnion,
            data.GetProperty("employed").GetBoolean(),
            data.TryGetProperty("employerName", out var employer) ? employer.GetString() : null,
            DateTimeOffset.UtcNow);
    }
}
