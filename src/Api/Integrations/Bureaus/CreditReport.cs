namespace Api.Integrations.Bureaus;

/// <summary>
/// Bureau-neutral credit summary. Validates its own invariants, so callers can
/// trust any instance they receive.
/// </summary>
public sealed class CreditReport
{
    public CreditReport(Bureau bureau, int score, int openAccounts, decimal totalBalance, DateTimeOffset retrievedAt)
    {
        if (score is < 300 or > 850)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Credit score must be between 300 and 850.");
        }

        if (openAccounts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openAccounts));
        }

        if (totalBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalBalance));
        }

        Bureau = bureau;
        Score = score;
        OpenAccounts = openAccounts;
        TotalBalance = totalBalance;
        RetrievedAt = retrievedAt;
    }

    public Bureau Bureau { get; }

    public int Score { get; }

    public int OpenAccounts { get; }

    public decimal TotalBalance { get; }

    public DateTimeOffset RetrievedAt { get; }
}
