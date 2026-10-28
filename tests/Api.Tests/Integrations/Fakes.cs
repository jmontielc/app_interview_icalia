using System.Net;
using System.Text;
using Api.Integrations.Secrets;

namespace Api.Tests.Integrations;

/// <summary>Records each outgoing request and replies with a canned response.</summary>
internal sealed class FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        Requests.Add(new CapturedRequest(request.RequestUri!, request.Headers.Authorization?.ToString(), body));
        return respond(request);
    }

    internal sealed record CapturedRequest(Uri Url, string? Authorization, string? Body);

    public static FakeHttpHandler Json(HttpStatusCode status, string json) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
}

internal sealed class FakeSecretProvider(string value) : ISecretProvider
{
    public int CallCount { get; private set; }

    public Task<string> GetSecretAsync(string name, CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult(value);
    }
}

/// <summary>Clock the test can move forward, to exercise cache expiry.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;
}
