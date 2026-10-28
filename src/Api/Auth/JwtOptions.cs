namespace Api.Auth;

/// <summary>Settings for issuing and validating access tokens. Bound from "Auth:Jwt".</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC key. Must be at least 32 bytes. Production loads it from the environment or a secret store.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 60;
}
