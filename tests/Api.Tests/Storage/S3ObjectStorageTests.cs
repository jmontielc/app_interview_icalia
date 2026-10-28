using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Api.Storage;
using Xunit;

namespace Api.Tests.Storage;

public class S3ObjectStorageTests
{
    private const string Key = "applicants/1/doc.pdf";

    // Signs locally, so no network or real credentials are needed.
    private static S3ObjectStorage Create(string? serviceUrl)
    {
        var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.USEast1 };
        if (serviceUrl is not null)
        {
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = true;
        }
        var s3 = new AmazonS3Client(new BasicAWSCredentials("test", "test"), config);
        var options = new StorageOptions { BucketName = "docs", S3ServiceUrl = serviceUrl };
        return new S3ObjectStorage(s3, options, TimeProvider.System);
    }

    [Fact]
    public void PresignedUrl_UsesHttp_WhenLocalEndpointIsPlainHttp()
    {
        var url = Create("http://localhost:4566").CreatePresignedGetUrl(Key, TimeSpan.FromMinutes(5));

        Assert.StartsWith("http://localhost:4566/docs/applicants/1/doc.pdf?", url);
    }

    [Fact]
    public void PresignedUrl_UsesHttps_ByDefault()
    {
        var url = Create(null).CreatePresignedGetUrl(Key, TimeSpan.FromMinutes(5));

        Assert.StartsWith("https://", url);
    }

    [Fact]
    public void PresignedUrl_RejectsLifetimeOverFiveMinutes() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(null).CreatePresignedGetUrl(Key, TimeSpan.FromMinutes(6)));
}
