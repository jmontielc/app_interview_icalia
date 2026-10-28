namespace Api.Security;

/// <summary>
/// Produces display-safe SSN text. Only the last four digits are kept.
/// Safe to call on partial or malformed input, so it can also redact logs.
/// </summary>
public static class SsnMasker
{
    public const string FullyMasked = "***-**-****";

    public static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return FullyMasked;
        }

        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        return digits.Length < 4
            ? FullyMasked
            : $"***-**-{digits[^4..]}";
    }
}
