using Api.Domain;
using Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace Api.Data;

/// <summary>
/// Development only. Brings the schema up to date and creates one user per role.
/// SQL Server uses the EF migrations. The SQLite test host has no migrations (they are SQL Server
/// specific), so it builds the schema from the model.
/// </summary>
public static class DevDataSeeder
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsSqlite())
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.MigrateAsync();
        }

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var password = configuration["Auth:DevUserPassword"]
            ?? throw new InvalidOperationException("Auth:DevUserPassword is not configured.");
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

        foreach (var (username, role) in new[]
                 {
                     ("admin", Roles.Admin),
                     ("loanofficer", Roles.LoanOfficer),
                     ("auditor", Roles.Auditor)
                 })
        {
            var user = new AppUser(username, role);
            user.SetPasswordHash(hasher.HashPassword(user, password));
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }
}
