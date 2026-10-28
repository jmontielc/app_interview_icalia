using Api.Security;

namespace Api.Domain;

/// <summary>
/// Loan applicant. The SSN is stored encrypted. Only the last four digits are kept in the clear,
/// for display.
/// </summary>
public sealed class Applicant
{
    private Applicant()
    {
    }

    private Applicant(string firstName, string lastName, string encryptedSsn, string ssnLast4, DateTimeOffset createdAt)
    {
        FirstName = firstName;
        LastName = lastName;
        EncryptedSsn = encryptedSsn;
        SsnLast4 = ssnLast4;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string EncryptedSsn { get; private set; } = string.Empty;

    public string SsnLast4 { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public string MaskedSsn => SsnMasker.Mask(SsnLast4);

    public static Applicant Create(string firstName, string lastName, string encryptedSsn, string ssnLast4, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        return new Applicant(firstName.Trim(), lastName.Trim(), encryptedSsn, ssnLast4, createdAt);
    }
}
