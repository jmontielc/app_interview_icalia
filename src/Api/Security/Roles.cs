namespace Api.Security;

public static class Roles
{
    public const string Admin = "Admin";
    public const string LoanOfficer = "LoanOfficer";
    public const string Auditor = "Auditor";
}

/// <summary>Named authorization policies. Controllers reference these, not raw role names.</summary>
public static class Policies
{
    /// <summary>Creates applicants and runs bureau checks.</summary>
    public const string CanRunCreditCheck = "CanRunCreditCheck";

    /// <summary>Reads applicants and reports. Auditors may read, not run checks.</summary>
    public const string CanViewReports = "CanViewReports";

    public const string CanViewAudit = "CanViewAudit";

    /// <summary>Uploads PDFs for an applicant.</summary>
    public const string CanManageDocuments = "CanManageDocuments";
}
