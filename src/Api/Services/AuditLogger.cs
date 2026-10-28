using Api.Data;
using Api.Domain;

namespace Api.Services;

/// <summary>Records who did what. Callers pass identifiers, never personal data.</summary>
public sealed class AuditLogger(AppDbContext db, TimeProvider clock)
{
    public async Task RecordAsync(string actor, string action, string? target, CancellationToken ct = default)
    {
        db.AuditEntries.Add(new AuditEntry(actor, action, target, clock.GetUtcNow()));
        await db.SaveChangesAsync(ct);
    }
}
