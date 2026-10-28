namespace Api.Security;

/// <summary>
/// Immutable, validated Social Security Number.
/// Display and serialization always use the masked form. The full digits are internal,
/// so only code in this assembly (such as the bureau client) can read them.
/// </summary>
public sealed class Ssn : IEquatable<Ssn>
{
    private readonly string _digits;

    private Ssn(string digits) => _digits = digits;

    public string Last4 => _digits[^4..];

    public string Masked => SsnMasker.Mask(_digits);

    internal string Digits => _digits;

    public static bool TryParse(string? input, out Ssn? ssn)
    {
        if (SsnValidator.Validate(input) != SsnValidationError.None)
        {
            ssn = null;
            return false;
        }

        ssn = new Ssn(SsnValidator.Normalize(input!));
        return true;
    }

    public bool Equals(Ssn? other) =>
        other is not null && string.Equals(_digits, other._digits, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is Ssn other && Equals(other);

    public override int GetHashCode() => _digits.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Masked;
}
