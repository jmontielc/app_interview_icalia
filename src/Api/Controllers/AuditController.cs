using Api.Contracts;
using Api.Data;
using Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.CanViewAudit)]
    public async Task<ActionResult<IReadOnlyList<AuditEntryResponse>>> List(CancellationToken ct)
    {
        var entries = await db.AuditEntries
            .AsNoTracking()
            // Id increases with insertion, which avoids ORDER BY on DateTimeOffset (unsupported by SQLite).
            .OrderByDescending(e => e.Id)
            .Take(200)
            .Select(e => new AuditEntryResponse(e.Id, e.OccurredAt, e.Actor, e.Action, e.Target))
            .ToListAsync(ct);

        return Ok(entries);
    }
}
