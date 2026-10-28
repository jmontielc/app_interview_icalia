using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Api.Auth;

/// <summary>Issues signed JWTs carrying the subject and role claims.</summary>
public sealed class TokenService(JwtOptions options, TimeProvider clock)
{
    public string Create(AppUser user)
    {
        var now = clock.GetUtcNow();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(AuthClaims.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(options.LifetimeMinutes).UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class AuthClaims
{
    public const string Role = "role";
}
