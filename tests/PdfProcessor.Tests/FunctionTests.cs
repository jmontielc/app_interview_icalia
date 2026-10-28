using System.Text;
using Amazon.Lambda.S3Events;
using AppInterview.Pdf;
using PdfProcessor;
using Xunit;

namespace PdfProcessor.Tests;

public class FunctionTests
{
    private const string Bucket = "app-interview-documents-local";

    private static S3Event EventFor(string key, long size, string bucket = Bucket) => new()
    {
        Records =
        [
            new S3Event.S3EventNotificationRecord
            {
                S3 = new S3Event.S3Entity
                {
                    Bucket = new S3Event.S3BucketEntity { Name = bucket },
                    Object = new S3Event.S3ObjectEntity { Key = key, Size = size }
                }
            }
        ]
    };

    [Fact]
    public async Task ValidPdf_IsTaggedPassed()
    {
        var store = new FakeObjectStore();
        var key = "applicants/1/doc.pdf";
        store.Objects[key] = Encoding.ASCII.GetBytes("%PDF-1.4 body");

        await new Function(store).ProcessAsync(EventFor(key, 13));

        Assert.Equal("completed", store.Tags[key]["processing"]);
        Assert.Equal("passed", store.Tags[key]["validation"]);
    }

    [Fact]
    public async Task NonPdfContent_IsTaggedFailed()
    {
        var store = new FakeObjectStore();
        var key = "applicants/1/malware.pdf";
        store.Objects[key] = Encoding.ASCII.GetBytes("MZ executable renamed to .pdf");

        var results = await new Function(store).ProcessAsync(EventFor(key, 29));

        Assert.Equal(PdfValidationResult.NotPdf, results[0].Result);
        Assert.Equal("completed", store.Tags[key]["processing"]);
        Assert.Equal("failed", store.Tags[key]["validation"]);
    }

    [Fact]
    public async Task OversizedObject_IsRejectedWithoutDownloading()
    {
        var store = new FakeObjectStore();
        var key = "applicants/1/huge.pdf";
        store.Objects[key] = Encoding.ASCII.GetBytes("%PDF-");

        var results = await new Function(store).ProcessAsync(EventFor(key, PdfRules.MaxBytes + 1));

        Assert.Equal(PdfValidationResult.TooLarge, results[0].Result);
        Assert.Equal(0, store.ReadCount);
        Assert.Equal("failed", store.Tags[key]["validation"]);
    }

    [Fact]
    public async Task EmptyObject_IsTaggedFailed()
    {
        var store = new FakeObjectStore();
        var key = "applicants/1/empty.pdf";
        store.Objects[key] = [];

        await new Function(store).ProcessAsync(EventFor(key, 0));

        Assert.Equal("failed", store.Tags[key]["validation"]);
    }

    [Fact]
    public async Task EventKey_IsUrlDecoded_WithPlusAsSpace()
    {
        var store = new FakeObjectStore();
        var key = "applicants/1/my report.pdf";
        store.Objects[key] = Encoding.ASCII.GetBytes("%PDF-1.7");

        await new Function(store).ProcessAsync(EventFor("applicants/1/my+report.pdf", 8));

        Assert.True(store.Tags.ContainsKey(key));
        Assert.Equal("passed", store.Tags[key]["validation"]);
    }

    [Fact]
    public async Task MultipleRecords_AreEachProcessed()
    {
        var store = new FakeObjectStore();
        store.Objects["a.pdf"] = Encoding.ASCII.GetBytes("%PDF-");
        store.Objects["b.pdf"] = Encoding.ASCII.GetBytes("nope");
        var input = EventFor("a.pdf", 5);
        input.Records.Add(EventFor("b.pdf", 4).Records[0]);

        var results = await new Function(store).ProcessAsync(input);

        Assert.Equal(2, results.Count);
        Assert.Equal("passed", store.Tags["a.pdf"]["validation"]);
        Assert.Equal("failed", store.Tags["b.pdf"]["validation"]);
    }
}

internal sealed class FakeObjectStore : IObjectStore
{
    public Dictionary<string, byte[]> Objects { get; } = new();

    public Dictionary<string, Dictionary<string, string>> Tags { get; } = new();

    public int ReadCount { get; private set; }

    public Task<byte[]> ReadAsync(string bucket, string key)
    {
        ReadCount++;
        return Task.FromResult(Objects[key]);
    }

    public Task TagAsync(string bucket, string key, IReadOnlyDictionary<string, string> tags)
    {
        Tags[key] = new Dictionary<string, string>(tags);
        return Task.CompletedTask;
    }
}
