using Api.Data;
using Api.Domain;
using Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>Creates and reads applicants. The only code path that decrypts stored SSNs.</summary>
public sealed class ApplicantService(AppDbContext db, ISsnProtector protector, TimeProvider clock)
{
    public async Task<Applicant> CreateAsync(string firstName, string lastName, Ssn ssn, CancellationToken ct = default)
    {
        var applicant = Applicant.Create(firstName, lastName, protector.Protect(ssn), ssn.Last4, clock.GetUtcNow());
        db.Applicants.Add(applicant);
        await db.SaveChangesAsync(ct);
        return applicant;
    }

    public Task<Applicant?> FindAsync(int id, CancellationToken ct = default) =>
        db.Applicants.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <summary>Decrypts the SSN for an outbound bureau call. Do not return it to clients.</summary>
    public Ssn UnprotectSsn(Applicant applicant) => protector.Unprotect(applicant.EncryptedSsn);
}
