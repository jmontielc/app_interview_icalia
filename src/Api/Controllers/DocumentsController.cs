using Api.Contracts;
using Api.Domain;
using Api.Security;
using Api.Services;
using AppInterview.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(DocumentService documents, AuditLogger audit) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.CanManageDocuments)]
    [RequestSizeLimit(PdfRules.MaxBytes + 64 * 1024)]
    public async Task<ActionResult<DocumentResponse>> Upload([FromQuery] int applicantId, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return ValidationProblem(detail: "The uploaded file is empty.");
        }

        using var buffer = new MemoryStream();
        await using (var stream = file.OpenReadStream())
        {
            await stream.CopyToAsync(buffer, ct);
        }

        var bytes = buffer.ToArray();
        var validation = PdfRules.Validate(bytes);
        if (validation != PdfValidationResult.Valid)
        {
            return ValidationProblem(detail: $"The file was rejected: {validation}.");
        }

        var record = await documents.UploadAsync(applicantId, file.FileName, bytes, ActorName, ct);
        if (record is null)
        {
            return NotFound();
        }

        await audit.RecordAsync(ActorName, "document.upload", $"document:{record.Id};applicant:{applicantId}", ct);

        return CreatedAtAction(nameof(Get), new { id = record.Id }, new DocumentResponse(record.Id, record.ApplicantId, record.FileName, record.SizeBytes, "pending", record.UploadedAt));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.CanViewReports)]
    public async Task<ActionResult<DocumentResponse>> Get(int id, CancellationToken ct)
    {
        var record = await documents.FindAsync(id, ct);
        if (record is null)
        {
            return NotFound();
        }

        var status = await documents.GetProcessingStatusAsync(record, ct);
        return Ok(new DocumentResponse(record.Id, record.ApplicantId, record.FileName, record.SizeBytes, status, record.UploadedAt));
    }

    /// <summary>Returns a link that expires after five minutes. Each use is audited.</summary>
    [HttpGet("{id:int}/url")]
    [Authorize(Policy = Policies.CanViewReports)]
    public async Task<ActionResult<DownloadLinkResponse>> CreateDownloadLink(int id, CancellationToken ct)
    {
        var record = await documents.FindAsync(id, ct);
        if (record is null)
        {
            return NotFound();
        }

        await audit.RecordAsync(ActorName, "document.download-link", $"document:{id}", ct);

        return Ok(new DownloadLinkResponse(documents.CreateDownloadUrl(record), DocumentService.DownloadLinkLifetime.TotalSeconds));
    }

    private string ActorName => User.Identity?.Name ?? "unknown";
}
