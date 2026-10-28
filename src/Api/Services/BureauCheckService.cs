using Api.Data;
using Api.Domain;
using Api.Integrations.Bureaus;

namespace Api.Services;

/// <summary>
/// Runs credit and employment checks against a bureau. Works through
/// <see cref="ICreditBureauClient"/>, so it does not depend on which bureau is picked.
/// </summary>
public sealed class BureauCheckService(
    IEnumerable<ICreditBureauClient> clients,
    ApplicantService applicants,
    AppDbContext db)
{
    /// <summary>Returns null when the applicant does not exist.</summary>
    public async Task<CreditReportRecord?> RunCreditCheckAsync(int applicantId, Bureau bureau, CancellationToken ct = default)
    {
        var applicant = await applicants.FindAsync(applicantId, ct);
        if (applicant is null)
        {
            return null;
        }

        var report = await ResolveClient(bureau).GetCreditReportAsync(applicants.UnprotectSsn(applicant), ct);
        var record = new CreditReportRecord(applicantId, report);
        db.CreditReports.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }

    /// <summary>Returns null when the applicant does not exist.</summary>
    public async Task<EmploymentStatus?> RunEmploymentCheckAsync(int applicantId, Bureau bureau, CancellationToken ct = default)
    {
        var applicant = await applicants.FindAsync(applicantId, ct);
        if (applicant is null)
        {
            return null;
        }

        return await ResolveClient(bureau).GetEmploymentStatusAsync(applicants.UnprotectSsn(applicant), ct);
    }

    private ICreditBureauClient ResolveClient(Bureau bureau) =>
        clients.SingleOrDefault(c => c.Bureau == bureau)
        ?? throw new InvalidOperationException($"No client is registered for {bureau}.");
}
