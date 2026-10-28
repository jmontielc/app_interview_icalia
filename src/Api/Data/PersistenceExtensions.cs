using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class PersistenceExtensions
{
    /// <summary>
    /// SQL Server by default. "Database:Provider"=Sqlite is used by the test host only.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (configuration["Database:Provider"] == "Sqlite")
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        return services;
    }
}
