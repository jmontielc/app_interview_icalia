using Api.Contracts;
using Api.Data;
using Api.Domain;
using Api.Integrations.Bureaus;
using Api.Security;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/credit-reports")]
public sealed class CreditReportsController(
    BureauCheckService checks,
    AppDbContext db,
    AuditLogger audit) : ControllerBase
{
    [HttpPost("{applicantId:int}")]
    [Authorize(Policy = Policies.CanRunCreditCheck)]
    public async Task<ActionResult<CreditReportResponse>> Run(int applicantId, [FromQuery] Bureau bureau, CancellationToken ct)
    {
        var record = await checks.RunCreditCheckAsync(applicantId, bureau, ct);
        if (record is null)
        {
            return NotFound();
        }

        await audit.RecordAsync(ActorName, "credit.check", $"applicant:{applicantId};bureau:{bureau}", ct);

        return CreatedAtAction(nameof(Get), new { id = record.Id }, ToResponse(record));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.CanViewReports)]
    public async Task<ActionResult<CreditReportResponse>> Get(int id, CancellationToken ct)
    {
        var record = await db.CreditReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        return record is null ? NotFound() : Ok(ToResponse(record));
    }

    private string ActorName => User.Identity?.Name ?? "unknown";

    private static CreditReportResponse ToResponse(CreditReportRecord r) =>
        new(r.Id, r.ApplicantId, r.Bureau.ToString(), r.Score, r.OpenAccounts, r.TotalBalance, r.RetrievedAt);
}
