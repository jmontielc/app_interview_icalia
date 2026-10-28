using Amazon.Runtime;
using Amazon.S3;

namespace Api.Storage;

public static class StorageExtensions
{
    public static IServiceCollection AddDocumentStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Storage").Get<StorageOptions>()
            ?? throw new InvalidOperationException("Storage is not configured.");
        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            throw new InvalidOperationException("Storage:BucketName is not configured.");
        }

        services.AddSingleton(options);
        services.AddSingleton<IAmazonS3>(_ => CreateS3Client(configuration));
        services.AddSingleton<IObjectStorage, S3ObjectStorage>();

        return services;
    }

    private static AmazonS3Client CreateS3Client(IConfiguration configuration)
    {
        // LocalStack needs a custom endpoint and path-style addressing. Production leaves ServiceURL empty.
        var serviceUrl = configuration["Aws:S3ServiceUrl"];
        if (string.IsNullOrEmpty(serviceUrl))
        {
            return new AmazonS3Client();
        }

        // LocalStack accepts any credentials. Placeholder keys are used only with a local endpoint, never in production.
        return new AmazonS3Client(
            new BasicAWSCredentials("local", "local"),
            new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true });
    }
}
