using Microsoft.AspNetCore.Authorization;

namespace Api.Security;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Every endpoint requires an authenticated user unless it is marked [AllowAnonymous].
    /// </summary>
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.CanRunCreditCheck, policy => policy.RequireRole(Roles.LoanOfficer, Roles.Admin))
            .AddPolicy(Policies.CanViewReports, policy => policy.RequireRole(Roles.LoanOfficer, Roles.Admin, Roles.Auditor))
            .AddPolicy(Policies.CanViewAudit, policy => policy.RequireRole(Roles.Auditor, Roles.Admin))
            .AddPolicy(Policies.CanManageDocuments, policy => policy.RequireRole(Roles.LoanOfficer, Roles.Admin));

        return services;
    }
}
