using Api.Security;

namespace Api.Integrations.Bureaus;

/// <summary>Contract shared by every bureau. Callers depend on this, not on a specific bureau.</summary>
public interface ICreditBureauClient
{
    Bureau Bureau { get; }

    Task<CreditReport> GetCreditReportAsync(Ssn ssn, CancellationToken ct = default);

    Task<EmploymentStatus> GetEmploymentStatusAsync(Ssn ssn, CancellationToken ct = default);
}
