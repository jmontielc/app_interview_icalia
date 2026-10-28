namespace Api.Security;

/// <summary>
/// Structural SSN rules set by the SSA. Accepts "123-45-6789", "123 45 6789" or "123456789".
/// Does not check that the SSN belongs to a real person.
/// </summary>
public static class SsnValidator
{
    private const int Length = 9;

    // Widely published invalid or sample numbers (advertising and wallet examples).
    private static readonly HashSet<string> KnownInvalid = new(StringComparer.Ordinal)
    {
        "078051120",
        "219099999"
    };

    public static SsnValidationError Validate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return SsnValidationError.Empty;
        }

        var digits = Normalize(input);
        if (digits.Length != Length || !digits.All(char.IsAsciiDigit))
        {
            return SsnValidationError.InvalidFormat;
        }

        var area = digits[..3];
        if (area == "000" || area == "666" || area[0] == '9')
        {
            return SsnValidationError.InvalidArea;
        }

        if (digits.Substring(3, 2) == "00")
        {
            return SsnValidationError.InvalidGroup;
        }

        if (digits[5..] == "0000")
        {
            return SsnValidationError.InvalidSerial;
        }

        return KnownInvalid.Contains(digits)
            ? SsnValidationError.KnownInvalid
            : SsnValidationError.None;
    }

    /// <summary>Removes the dashes and spaces users type. Does not validate.</summary>
    public static string Normalize(string input) =>
        input.Replace("-", string.Empty).Replace(" ", string.Empty);
}
