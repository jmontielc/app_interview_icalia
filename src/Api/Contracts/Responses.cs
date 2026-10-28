using Api.Security;

namespace Api.Contracts;

// Request and response shapes. Only masked SSNs leave the API.

public sealed record CreateApplicantRequest(string FirstName, string LastName, Ssn Ssn);

public sealed record ApplicantResponse(int Id, string FirstName, string LastName, string MaskedSsn);

public sealed record CreditReportResponse(
    int Id,
    int ApplicantId,
    string Bureau,
    int Score,
    int OpenAccounts,
    decimal TotalBalance,
    DateTimeOffset RetrievedAt);

public sealed record EmploymentResponse(
    int ApplicantId,
    string Bureau,
    bool IsEmployed,
    string? EmployerName,
    DateTimeOffset AsOf);

public sealed record AuditEntryResponse(int Id, DateTimeOffset OccurredAt, string Actor, string Action, string? Target);
