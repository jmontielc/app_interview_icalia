using System.Net;
using Api.Integrations.Bureaus;
using Api.Integrations.Secrets;
using Api.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.Tests.Integrations;

public class BureauClientTests
{
    private static readonly Uri EquifaxBase = new("https://equifax.test/");
    private static readonly Uri TransUnionBase = new("https://transunion.test/");

    private static Ssn ValidSsn()
    {
        Ssn.TryParse("123-45-6789", out var ssn);
        return ssn!;
    }

    private static (EquifaxClient Client, FakeHttpHandler Handler) Equifax(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = FakeHttpHandler.Json(status, json);
        var client = new EquifaxClient(
            new HttpClient(handler) { BaseAddress = EquifaxBase },
            new FakeSecretProvider("test-token"));
        return (client, handler);
    }

    private static (TransUnionClient Client, FakeHttpHandler Handler) TransUnion(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = FakeHttpHandler.Json(status, json);
        var client = new TransUnionClient(
            new HttpClient(handler) { BaseAddress = TransUnionBase },
            new FakeSecretProvider("test-token"));
        return (client, handler);
    }

    private const string EquifaxCreditJson = """{"creditScore":720,"openAccounts":3,"totalBalance":1234.50}""";
    private const string TransUnionCreditJson = """{"data":{"score":{"value":650},"tradelines":{"openCount":5},"balances":{"total":2000.00}}}""";

    [Fact]
    public async Task Equifax_CreditReport_MapsFieldsAndSendsBearerToken()
    {
        var (client, handler) = Equifax(EquifaxCreditJson);

        var report = await client.GetCreditReportAsync(ValidSsn());

        Assert.Equal(Bureau.Equifax, report.Bureau);
        Assert.Equal(720, report.Score);
        Assert.Equal(3, report.OpenAccounts);
        Assert.Equal(1234.50m, report.TotalBalance);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://equifax.test/v1/credit-reports", request.Url.ToString());
        Assert.Equal("Bearer test-token", request.Authorization);
    }

    [Fact]
    public async Task Equifax_CreditReport_SendsSsnInBodyNotUrl()
    {
        var (client, handler) = Equifax(EquifaxCreditJson);

        await client.GetCreditReportAsync(ValidSsn());

        var request = Assert.Single(handler.Requests);
        Assert.Contains("123456789", request.Body);
        Assert.DoesNotContain("123456789", request.Url.ToString());
    }

    [Fact]
    public async Task TransUnion_CreditReport_MapsNestedEnvelope()
    {
        var (client, handler) = TransUnion(TransUnionCreditJson);

        var report = await client.GetCreditReportAsync(ValidSsn());

        Assert.Equal(Bureau.TransUnion, report.Bureau);
        Assert.Equal(650, report.Score);
        Assert.Equal(5, report.OpenAccounts);
        Assert.Equal(2000.00m, report.TotalBalance);
        Assert.Equal("Bearer test-token", Assert.Single(handler.Requests).Authorization);
    }

    [Fact]
    public async Task Equifax_Employment_ActiveStatusIsEmployed()
    {
        var (client, _) = Equifax("""{"employmentStatus":"ACTIVE","employer":"Acme"}""");

        var status = await client.GetEmploymentStatusAsync(ValidSsn());

        Assert.True(status.IsEmployed);
        Assert.Equal("Acme", status.EmployerName);
    }

    [Fact]
    public async Task TransUnion_Employment_MapsEmployedFlag()
    {
        var (client, _) = TransUnion("""{"data":{"employed":false}}""");

        var status = await client.GetEmploymentStatusAsync(ValidSsn());

        Assert.False(status.IsEmployed);
        Assert.Null(status.EmployerName);
        Assert.Equal(Bureau.TransUnion, status.Bureau);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task HttpError_ThrowsBureauExceptionWithStatusOnly(HttpStatusCode status)
    {
        var (client, _) = Equifax("""{"error":"123456789 is not valid for this subject"}""", status);

        var ex = await Assert.ThrowsAsync<BureauException>(() => client.GetCreditReportAsync(ValidSsn()));

        Assert.Equal((int)status, ex.StatusCode);
        Assert.Equal(Bureau.Equifax, ex.Bureau);
        // The response body echoes the SSN in this test. It must not reach the exception.
        Assert.DoesNotContain("123456789", ex.Message);
    }

    [Fact]
    public async Task UnexpectedShape_ThrowsBureauException()
    {
        var (client, _) = TransUnion("""{"unexpected":true}""");

        var ex = await Assert.ThrowsAsync<BureauException>(() => client.GetCreditReportAsync(ValidSsn()));

        Assert.Null(ex.StatusCode);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task OutOfRangeScore_ThrowsBureauException()
    {
        var (client, _) = Equifax("""{"creditScore":999,"openAccounts":1,"totalBalance":0}""");

        await Assert.ThrowsAsync<BureauException>(() => client.GetCreditReportAsync(ValidSsn()));
    }

    [Fact]
    public async Task Clients_AreUsableThroughTheSharedContract()
    {
        var equifax = Equifax(EquifaxCreditJson).Client;
        var transUnion = TransUnion(TransUnionCreditJson).Client;
        var clients = new List<ICreditBureauClient> { equifax, transUnion };

        var reports = new List<CreditReport>();
        foreach (var client in clients)
        {
            reports.Add(await client.GetCreditReportAsync(ValidSsn()));
        }

        Assert.Equal([Bureau.Equifax, Bureau.TransUnion], reports.Select(r => r.Bureau));
    }
}

public class SecretProviderTests
{
    [Fact]
    public async Task Cached_ReturnsCachedValueWithinTtl()
    {
        var inner = new FakeSecretProvider("v1");
        var clock = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var cached = new CachedSecretProvider(inner, clock, TimeSpan.FromMinutes(5));

        await cached.GetSecretAsync("s");
        clock.Now = clock.Now.AddMinutes(4);
        await cached.GetSecretAsync("s");

        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task Cached_RefetchesAfterTtlExpires()
    {
        var inner = new FakeSecretProvider("v1");
        var clock = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var cached = new CachedSecretProvider(inner, clock, TimeSpan.FromMinutes(5));

        await cached.GetSecretAsync("s");
        clock.Now = clock.Now.AddMinutes(6);
        await cached.GetSecretAsync("s");

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task Config_ReadsSecretFromSecretsSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:bureau/equifax/api-token"] = "abc" })
            .Build();

        var value = await new ConfigSecretProvider(config).GetSecretAsync("bureau/equifax/api-token");

        Assert.Equal("abc", value);
    }

    [Fact]
    public async Task Config_MissingSecret_Throws()
    {
        var config = new ConfigurationBuilder().Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ConfigSecretProvider(config).GetSecretAsync("missing"));
    }
}
