using Microsoft.AspNetCore.DataProtection;

namespace Api.Security;

/// <summary>
/// Encrypts SSNs at rest with ASP.NET Data Protection. Production should back the keys with
/// a KMS-protected store, not the default local key ring.
/// </summary>
public interface ISsnProtector
{
    string Protect(Ssn ssn);

    Ssn Unprotect(string protectedValue);
}

public sealed class SsnProtector(IDataProtectionProvider provider) : ISsnProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Applicant.Ssn.v1");

    public string Protect(Ssn ssn) => _protector.Protect(ssn.Digits);

    public Ssn Unprotect(string protectedValue)
    {
        var digits = _protector.Unprotect(protectedValue);
        if (!Ssn.TryParse(digits, out var ssn))
        {
            throw new InvalidOperationException("Stored SSN failed validation.");
        }

        return ssn!;
    }
}
