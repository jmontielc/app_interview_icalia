using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Applicant> Applicants => Set<Applicant>();

    public DbSet<CreditReportRecord> CreditReports => Set<CreditReportRecord>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Applicant>().Property(a => a.EncryptedSsn).HasMaxLength(512);
        modelBuilder.Entity<CreditReportRecord>().Property(r => r.TotalBalance).HasPrecision(18, 2);
        modelBuilder.Entity<CreditReportRecord>().HasIndex(r => r.ApplicantId);
    }
}
