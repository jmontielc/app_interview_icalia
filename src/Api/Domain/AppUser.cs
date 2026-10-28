namespace Api.Domain;

/// <summary>An operator who signs in to the app. Passwords are stored only as hashes.</summary>
public sealed class AppUser
{
    private AppUser()
    {
    }

    public AppUser(string username, string role)
    {
        Username = username;
        Role = role;
    }

    public int Id { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
