using Api.Integrations.Bureaus;

namespace Api.Domain;

/// <summary>A stored bureau credit summary for one applicant.</summary>
public sealed class CreditReportRecord
{
    private CreditReportRecord()
    {
    }

    public CreditReportRecord(int applicantId, CreditReport report)
    {
        ApplicantId = applicantId;
        Bureau = report.Bureau;
        Score = report.Score;
        OpenAccounts = report.OpenAccounts;
        TotalBalance = report.TotalBalance;
        RetrievedAt = report.RetrievedAt;
    }

    public int Id { get; private set; }

    public int ApplicantId { get; private set; }

    public Bureau Bureau { get; private set; }

    public int Score { get; private set; }

    public int OpenAccounts { get; private set; }

    public decimal TotalBalance { get; private set; }

    public DateTimeOffset RetrievedAt { get; private set; }
}
