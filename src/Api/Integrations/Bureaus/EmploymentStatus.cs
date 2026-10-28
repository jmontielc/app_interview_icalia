namespace Api.Integrations.Bureaus;

/// <summary>Bureau-neutral employment verification result.</summary>
public sealed class EmploymentStatus
{
    public EmploymentStatus(Bureau bureau, bool isEmployed, string? employerName, DateTimeOffset asOf)
    {
        Bureau = bureau;
        IsEmployed = isEmployed;
        EmployerName = employerName;
        AsOf = asOf;
    }

    public Bureau Bureau { get; }

    public bool IsEmployed { get; }

    public string? EmployerName { get; }

    public DateTimeOffset AsOf { get; }
}
