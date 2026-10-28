using Api.Domain;
using Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

/// <summary>
/// Development only. Creates the schema and one user per role. Migrations are not set up yet.
/// </summary>
public static class DevDataSeeder
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

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
