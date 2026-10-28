using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Api.Tests.Endpoints;

public class DocumentTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly byte[] SamplePdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% sample\n%%EOF");

    private async Task<HttpClient> ClientForAsync(string username)
    {
        var anonymous = factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/token", new { username, password = "Local-Dev-Password-1!" });
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
            new { firstName = "Doc", lastName = "Owner", ssn = "123-45-6789" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static MultipartFormDataContent PdfUpload(byte[] content, string fileName = "report.pdf")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);
        return form;
    }

    [Fact]
    public async Task LoanOfficer_UploadsPdf_StoredUnderServerGeneratedKey()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var applicantId = await CreateApplicantAsync(loanOfficer);

        var response = await loanOfficer.PostAsync($"/api/documents?applicantId={applicantId}", PdfUpload(SamplePdf, "../../evil name.pdf"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("evil name.pdf", body.GetProperty("fileName").GetString());
        Assert.Equal("pending", body.GetProperty("processingStatus").GetString());

        var key = Assert.Single(factory.Storage.Keys, k => k.StartsWith($"applicants/{applicantId}/"));
        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain("evil", key);
    }

    [Fact]
    public async Task NonPdfContent_IsRejected()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var applicantId = await CreateApplicantAsync(loanOfficer);

        var response = await loanOfficer.PostAsync($"/api/documents?applicantId={applicantId}",
            PdfUpload(Encoding.ASCII.GetBytes("MZ this is an executable")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Auditor_CannotUpload()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var auditor = await ClientForAsync("auditor");
        var applicantId = await CreateApplicantAsync(loanOfficer);

        var response = await auditor.PostAsync($"/api/documents?applicantId={applicantId}", PdfUpload(SamplePdf));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DownloadLink_IsShortLivedAndAudited()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var auditor = await ClientForAsync("auditor");
        var applicantId = await CreateApplicantAsync(loanOfficer);
        var upload = await loanOfficer.PostAsync($"/api/documents?applicantId={applicantId}", PdfUpload(SamplePdf));
        var documentId = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var link = await auditor.GetAsync($"/api/documents/{documentId}/url");

        Assert.Equal(HttpStatusCode.OK, link.StatusCode);
        var body = await link.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("X-Amz-Expires=300", body.GetProperty("url").GetString());
        Assert.Equal(300, body.GetProperty("expiresInSeconds").GetDouble());
        Assert.Contains(TimeSpan.FromMinutes(5), factory.Storage.PresignLifetimes);

        var audit = await auditor.GetStringAsync("/api/audit");
        Assert.Contains("document.download-link", audit);
    }

    [Fact]
    public async Task ProcessingStatus_ReportsRejected_WhenLambdaValidationFails()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var applicantId = await CreateApplicantAsync(loanOfficer);
        var upload = await loanOfficer.PostAsync($"/api/documents?applicantId={applicantId}", PdfUpload(SamplePdf));
        var documentId = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var key = factory.Storage.Keys.Single(k => k.StartsWith($"applicants/{applicantId}/"));
        factory.Storage.SetTag(key, "processing", "completed");
        factory.Storage.SetTag(key, "validation", "failed");

        var status = await loanOfficer.GetFromJsonAsync<JsonElement>($"/api/documents/{documentId}");
        Assert.Equal("rejected", status.GetProperty("processingStatus").GetString());
    }

    [Fact]
    public async Task ProcessingStatus_ReflectsLambdaTags()
    {
        var loanOfficer = await ClientForAsync("loanofficer");
        var applicantId = await CreateApplicantAsync(loanOfficer);
        var upload = await loanOfficer.PostAsync($"/api/documents?applicantId={applicantId}", PdfUpload(SamplePdf));
        var body = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var documentId = body.GetProperty("id").GetInt32();

        var key = factory.Storage.Keys.Single(k => k.StartsWith($"applicants/{applicantId}/"));
        factory.Storage.SetTag(key, "processing", "completed");
        factory.Storage.SetTag(key, "validation", "passed");

        var status = await loanOfficer.GetFromJsonAsync<JsonElement>($"/api/documents/{documentId}");
        Assert.Equal("completed", status.GetProperty("processingStatus").GetString());
    }
}
