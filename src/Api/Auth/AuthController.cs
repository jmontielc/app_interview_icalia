using Api.Data;
using Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    TokenService tokens,
    IPasswordHasher<AppUser> hasher,
    IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// Development-only sign-in. Production uses the identity provider (Cognito/OIDC), so this returns 404 elsewhere.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> IssueToken([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        return Ok(new TokenResponse(tokens.Create(user), user.Role));
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record TokenResponse(string AccessToken, string Role);
