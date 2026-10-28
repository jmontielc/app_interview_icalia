using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Api.Tests.Endpoints;

public class AuthorizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string ValidSsn = "123-45-6789";

    private readonly HttpClient _anonymous = factory.CreateClient();

    private async Task<HttpClient> ClientForAsync(string username)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/token", new { username, password = "Local-Dev-Password-1!" });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
        return client;
    }

    private async Task<int> CreateApplicantAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/applicants",
            new { firstName = "Ada", lastName = "Lovelace", ssn = ValidSsn });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Unauthenticated_CannotCreateApplicant()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/applicants",
            new { firstName = "A", lastName = "B", ssn = ValidSsn });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Auditor_CannotCreateApplicant()
    {
        var auditor = await ClientForAsync("auditor");

        var response = await auditor.PostAsJsonAsync("/api/applicants",
            new { firstName = "A", lastName = "B", ssn = ValidSsn });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LoanOfficer_CreatesApplicant_ResponseHasOnlyMaskedSsn()
    {
        var loanOfficer = await ClientForAsync("loanofficer");

        var response = await loanOfficer.PostAsJsonAsync("/api/applicants",
            new { firstName = "Grace", lastName = "Hopper", ssn = ValidSsn });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("***-**-6789", raw);
        Assert.DoesNotContain("123-45-6789", raw);
        Assert.DoesNotContain("123456789", raw);
    }

    [Fact]
    public async Task InvalidSsn_IsRejected()
    {
        var loanOfficer = await ClientForAsync("loanofficer");

        var response = await loanOfficer.PostAsJsonAsync("/api/applicants",
            new { firstName = "A", lastName = "B", ssn = "000-12-3456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoanOfficer_RunsCreditCheck_AndAuditorCanRead()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var auditor = await ClientForAsync("auditor");
        var applicantId = await CreateApplicantAsync(loanOfficer);

        var check = await loanOfficer.PostAsync($"/api/credit-reports/{applicantId}?bureau=equifax", null);
        Assert.Equal(HttpStatusCode.Created, check.StatusCode);

        var reportId = (await check.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var read = await auditor.GetAsync($"/api/credit-reports/{reportId}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task Auditor_CannotRunCreditCheck()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var auditor = await ClientForAsync("auditor");
        var applicantId = await CreateApplicantAsync(loanOfficer);

        var response = await auditor.PostAsync($"/api/credit-reports/{applicantId}?bureau=equifax", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnknownBureau_IsRejected()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var applicantId = await CreateApplicantAsync(loanOfficer);

        var response = await loanOfficer.PostAsync($"/api/credit-reports/{applicantId}?bureau=experian", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmploymentCheck_ReturnsStatus_ForLoanOfficer()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var applicantId = await CreateApplicantAsync(loanOfficer);

        var response = await loanOfficer.PostAsync($"/api/applicants/{applicantId}/employment-check?bureau=transunion", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isEmployed").GetBoolean());
    }

    [Fact]
    public async Task AuditLog_VisibleToAuditor_NotToLoanOfficer()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var auditor = await ClientForAsync("auditor");
        await CreateApplicantAsync(loanOfficer);

        var asAuditor = await auditor.GetAsync("/api/audit");
        var asLoanOfficer = await loanOfficer.GetAsync("/api/audit");

        Assert.Equal(HttpStatusCode.OK, asAuditor.StatusCode);
        Assert.Contains("applicant.create", await asAuditor.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, asLoanOfficer.StatusCode);
    }

    [Fact]
    public async Task TokenEndpoint_RejectsWrongPassword()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/token",
            new { username = "admin", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
