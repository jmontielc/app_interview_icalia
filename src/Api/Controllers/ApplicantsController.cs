using Api.Contracts;
using Api.Domain;
using Api.Integrations.Bureaus;
using Api.Security;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/applicants")]
public sealed class ApplicantsController(
    ApplicantService applicants,
    BureauCheckService checks,
    AuditLogger audit) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.CanRunCreditCheck)]
    public async Task<ActionResult<ApplicantResponse>> Create([FromBody] CreateApplicantRequest request, CancellationToken ct)
    {
        var applicant = await applicants.CreateAsync(request.FirstName, request.LastName, request.Ssn, ct);
        await audit.RecordAsync(ActorName, "applicant.create", $"applicant:{applicant.Id}", ct);

        return CreatedAtAction(nameof(Get), new { id = applicant.Id }, ToResponse(applicant));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.CanViewReports)]
    public async Task<ActionResult<ApplicantResponse>> Get(int id, CancellationToken ct)
    {
        var applicant = await applicants.FindAsync(id, ct);
        return applicant is null ? NotFound() : Ok(ToResponse(applicant));
    }

    [HttpPost("{id:int}/employment-check")]
    [Authorize(Policy = Policies.CanRunCreditCheck)]
    public async Task<ActionResult<EmploymentResponse>> EmploymentCheck(int id, [FromQuery] Bureau bureau, CancellationToken ct)
    {
        var status = await checks.RunEmploymentCheckAsync(id, bureau, ct);
        if (status is null)
        {
            return NotFound();
        }

        await audit.RecordAsync(ActorName, "employment.check", $"applicant:{id};bureau:{bureau}", ct);

        return Ok(new EmploymentResponse(id, bureau.ToString(), status.IsEmployed, status.EmployerName, status.AsOf));
    }

    private string ActorName => User.Identity?.Name ?? "unknown";

    private static ApplicantResponse ToResponse(Applicant applicant) =>
        new(applicant.Id, applicant.FirstName, applicant.LastName, applicant.MaskedSsn);
}
