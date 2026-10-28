using Amazon.SecretsManager;
using Api.Integrations.Bureaus;
using Api.Integrations.Secrets;

namespace Api.Integrations;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers secrets and both bureau clients. The secret provider comes from
    /// configuration: "Config" for local development, "AwsSecretsManager" elsewhere.
    /// </summary>
    public static IServiceCollection AddBureauIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<ISecretProvider>(sp =>
        {
            var clock = sp.GetRequiredService<TimeProvider>();
            ISecretProvider inner = configuration["Secrets:Provider"] == "AwsSecretsManager"
                ? new AwsSecretsManagerProvider(CreateSecretsManagerClient(configuration))
                : new ConfigSecretProvider(configuration);

            return new CachedSecretProvider(inner, clock, TimeSpan.FromMinutes(5));
        });

        services.AddHttpClient<EquifaxClient>(http =>
                http.BaseAddress = new Uri(configuration["Bureaus:Equifax:BaseUrl"]
                    ?? throw new InvalidOperationException("Bureaus:Equifax:BaseUrl is not configured.")))
            .AddStandardResilienceHandler();

        services.AddHttpClient<TransUnionClient>(http =>
                http.BaseAddress = new Uri(configuration["Bureaus:TransUnion:BaseUrl"]
                    ?? throw new InvalidOperationException("Bureaus:TransUnion:BaseUrl is not configured.")))
            .AddStandardResilienceHandler();

        // Typed HttpClients are transient, so the bureau clients are too.
        services.AddTransient<ICreditBureauClient>(sp => sp.GetRequiredService<EquifaxClient>());
        services.AddTransient<ICreditBureauClient>(sp => sp.GetRequiredService<TransUnionClient>());

        return services;
    }

    private static AmazonSecretsManagerClient CreateSecretsManagerClient(IConfiguration configuration)
    {
        // LocalStack endpoint is set only in local development.
        var serviceUrl = configuration["Aws:SecretsManagerServiceUrl"];
        return string.IsNullOrEmpty(serviceUrl)
            ? new AmazonSecretsManagerClient()
            // LocalStack accepts any credentials. Placeholder keys are used only with a local endpoint, never in production.
            : new AmazonSecretsManagerClient(
                new Amazon.Runtime.BasicAWSCredentials("local", "local"),
                new AmazonSecretsManagerConfig { ServiceURL = serviceUrl });
    }
}
