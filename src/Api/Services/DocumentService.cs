using Api.Data;
using Api.Domain;
using Api.Storage;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class DocumentService(AppDbContext db, IObjectStorage storage, TimeProvider clock)
{
    public static readonly TimeSpan DownloadLinkLifetime = TimeSpan.FromMinutes(5);

    /// <summary>Returns null when the applicant does not exist. The caller must validate the PDF first.</summary>
    public async Task<DocumentRecord?> UploadAsync(int applicantId, string fileName, byte[] content, string uploadedBy, CancellationToken ct = default)
    {
        if (!await db.Applicants.AnyAsync(a => a.Id == applicantId, ct))
        {
            return null;
        }

        var objectKey = $"applicants/{applicantId}/{Guid.NewGuid():N}.pdf";
        await storage.PutAsync(
            objectKey,
            content,
            "application/pdf",
            new Dictionary<string, string> { ["processing"] = "pending" },
            ct);

        var record = new DocumentRecord(applicantId, objectKey, SafeFileName(fileName), content.LongLength, uploadedBy, clock.GetUtcNow());
        db.Documents.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }

    public Task<DocumentRecord?> FindAsync(int id, CancellationToken ct = default) =>
        db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    /// <summary>
    /// Reads the tags the PdfProcessor Lambda writes. Returns "pending" until it has run,
    /// then "completed" or "rejected" depending on validation.
    /// </summary>
    public async Task<string> GetProcessingStatusAsync(DocumentRecord document, CancellationToken ct = default)
    {
        var tags = await storage.GetTagsAsync(document.ObjectKey, ct);
        if (!tags.TryGetValue("processing", out var processing) || processing != "completed")
        {
            return "pending";
        }

        return tags.TryGetValue("validation", out var validation) && validation == "failed"
            ? "rejected"
            : "completed";
    }

    public string CreateDownloadUrl(DocumentRecord document) =>
        storage.CreatePresignedGetUrl(document.ObjectKey, DownloadLinkLifetime);

    /// <summary>Keeps only the base name and removes characters that cause trouble in headers and logs.</summary>
    private static string SafeFileName(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty);
        var cleaned = new string(name.Where(c => !char.IsControl(c) && c != '"').ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            return "document.pdf";
        }

        return cleaned.Length > 255 ? cleaned[^255..] : cleaned;
    }
}
