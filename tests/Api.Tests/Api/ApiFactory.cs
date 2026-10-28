using Api.Data;
using Api.Domain;
using Api.Integrations.Bureaus;
using Api.Security;
using Api.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests.Endpoints;

/// <summary>
/// Hosts the real API on a throwaway SQLite file, with bureau clients and S3 replaced by in-memory stubs.
/// Runs as Development so the dev token endpoint and seeded users are available.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"app-interview-tests-{Guid.NewGuid():N}.db");

    /// <summary>The storage the API writes to during the test, so tests can inspect it.</summary>
    public InMemoryObjectStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICreditBureauClient>();
            services.AddTransient<ICreditBureauClient>(_ => new StubBureau(Bureau.Equifax));
            services.AddTransient<ICreditBureauClient>(_ => new StubBureau(Bureau.TransUnion));

            services.RemoveAll<IObjectStorage>();
            services.AddSingleton<IObjectStorage>(Storage);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Release pooled SQLite handles so the temp file can be deleted.
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // The OS still holds the file. The temp directory is cleaned up eventually.
        }
    }

    private sealed class StubBureau(Bureau bureau) : ICreditBureauClient
    {
        public Bureau Bureau { get; } = bureau;

        public Task<CreditReport> GetCreditReportAsync(Ssn ssn, CancellationToken ct = default) =>
            Task.FromResult(new CreditReport(Bureau, 700, 2, 500m, DateTimeOffset.UtcNow));

        public Task<EmploymentStatus> GetEmploymentStatusAsync(Ssn ssn, CancellationToken ct = default) =>
            Task.FromResult(new EmploymentStatus(Bureau, true, "Stub Employer", DateTimeOffset.UtcNow));
    }
}
